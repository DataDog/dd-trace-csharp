// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.

#include "cor_profiler.h"
#include <fstream>
#include <string>
#include <vector>
#include "ComPtr.h"
#include "ILRewriter.h"
#include "Macros.h"
#include "ModuleMetadata.h"
#include "clr_helpers.h"
#include "integration_loader.h"
#include "metadata_builder.h"
#include "util.h"

namespace trace {

CorProfiler* profiler = nullptr;

CorProfiler::CorProfiler() : integrations_(LoadIntegrationsFromEnvironment()) {}

HRESULT STDMETHODCALLTYPE
CorProfiler::Initialize(IUnknown* cor_profiler_info_unknown) {
  is_attached_ = FALSE;

  auto process_name = GetCurrentProcessName();
  auto allowed_process_names = GetEnvironmentValues(kProcessesEnvironmentName);

  if (allowed_process_names.size() == 0) {
    LOG_APPEND(
        L"DATADOG_PROFILER_PROCESSES environment variable not set. Attaching "
        L"to any .NET process.");
  } else {
    LOG_APPEND(L"DATADOG_PROFILER_PROCESSES:");
    for (auto& name : allowed_process_names) {
      LOG_APPEND(L"  " + name);
    }

    if (std::find(allowed_process_names.begin(), allowed_process_names.end(),
                  process_name) == allowed_process_names.end()) {
      LOG_APPEND(L"CorProfiler disabled: module name \""
                 << process_name
                 << "\" does not match DATADOG_PROFILER_PROCESSES environment "
                    "variable.");
      return E_FAIL;
    }
  }

  HRESULT hr = cor_profiler_info_unknown->QueryInterface<ICorProfilerInfo3>(
      &this->info_);
  LOG_IFFAILEDRET(hr,
                  L"CorProfiler disabled: interface ICorProfilerInfo3 or "
                  L"higher not found.");

  hr = this->info_->SetEventMask(kEventMask);
  LOG_IFFAILEDRET(hr, L"Failed to attach profiler: unable to set event mask.");

  // we're in!
  LOG_APPEND(L"CorProfiler attached to process " << process_name);
  this->info_->AddRef();
  is_attached_ = true;
  profiler = this;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadFinished(ModuleID module_id,
                                                          HRESULT hrStatus) {
  auto module_info = GetModuleInfo(this->info_, module_id);
  if (!module_info.IsValid()) {
    return S_OK;
  }

  if (module_info.IsWindowsRuntime() ||
      module_info.assembly.name == L"mscorlib") {
    // We cannot obtain writeable metadata interfaces on Windows Runtime modules
    // or instrument their IL. We must never try to add assembly references to
    // mscorlib.
    LOG_APPEND(L"ModuleLoadFinished() called for "
               << module_info.assembly.name << ". Skipping instrumentation.");
    return S_OK;
  }

  std::vector<Integration> enabled_integrations =
      FilterIntegrationsByCaller(integrations_, module_info.assembly.name);
  if (enabled_integrations.empty()) {
    // we don't need to instrument anything in this module, skip it
    LOG_APPEND(L"ModuleLoadFinished() called for "
               << module_info.assembly.name << ". Nothing to instrument.");
    return S_OK;
  }

  LOG_APPEND(L"ModuleLoadFinished() called for "
             << module_info.assembly.name
             << ". Emitting instrumentation metadata.");

  ComPtr<IUnknown> metadata_interfaces;

  auto hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite,
                                           IID_IMetaDataImport,
                                           metadata_interfaces.GetAddressOf());

  LOG_IFFAILEDRET(hr, L"Failed to get metadata interface.");

  const auto metadata_import =
      metadata_interfaces.As<IMetaDataImport>(IID_IMetaDataImport);
  const auto metadata_emit =
      metadata_interfaces.As<IMetaDataEmit>(IID_IMetaDataEmit);
  const auto assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(
      IID_IMetaDataAssemblyImport);
  const auto assembly_emit =
      metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

  enabled_integrations =
      FilterIntegrationsByTarget(enabled_integrations, assembly_import);
  if (enabled_integrations.empty()) {
    // we don't need to instrument anything in this module, skip it
    return S_OK;
  }

  mdModule module;
  hr = metadata_import->GetModuleFromScope(&module);
  LOG_IFFAILEDRET(hr, L"Failed to get module token.");

  ModuleMetadata* module_metadata = new ModuleMetadata(
      metadata_import, module_info.assembly.name, enabled_integrations);

  MetadataBuilder metadata_builder(*module_metadata, module, metadata_import,
                                   metadata_emit, assembly_import,
                                   assembly_emit);

  for (const auto& integration : enabled_integrations) {
    for (const auto& method_replacement : integration.method_replacements) {
      // for each wrapper assembly, emit an assembly reference
      hr = metadata_builder.EmitAssemblyRef(
          method_replacement.wrapper_method.assembly);
      RETURN_OK_IF_FAILED(hr);

      // for each method replacement in each enabled integration,
      // emit a reference to the instrumentation wrapper methods
      hr = metadata_builder.StoreWrapperMethodRef(method_replacement);
      RETURN_OK_IF_FAILED(hr);
    }
  }

  // store module info for later lookup
  module_id_to_info_map_.Update(module_id, module_metadata);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleUnloadFinished(ModuleID moduleId,
                                                            HRESULT hrStatus) {
  ModuleMetadata* metadata;

  if (module_id_to_info_map_.LookupIfExists(moduleId, &metadata)) {
    module_id_to_info_map_.Erase(moduleId);
    delete metadata;
  }

  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStarted(
    FunctionID function_id, BOOL is_safe_to_block) {
  ClassID class_id;
  ModuleID module_id;
  mdToken function_token = mdTokenNil;

  HRESULT hr = this->info_->GetFunctionInfo(function_id, &class_id, &module_id,
                                            &function_token);
  RETURN_OK_IF_FAILED(hr);

  ModuleMetadata* module_metadata = nullptr;

  if (!module_id_to_info_map_.LookupIfExists(module_id, &module_metadata)) {
    // we haven't stored a ModuleInfo for this module, so we can't modify its
    // IL
    return S_OK;
  }

  // get function info
  auto caller =
      GetFunctionInfo(module_metadata->metadata_import, function_token);
  if (!caller.IsValid()) {
    return S_OK;
  }

  auto method_replacements =
      module_metadata->GetMethodReplacementsForCaller(caller);
  if (method_replacements.empty()) {
    return S_OK;
  }

  ILRewriter rewriter(this->info_, nullptr, module_id, function_token);
  bool modified = false;

  // hr = rewriter.Initialize();
  hr = rewriter.Import();
  RETURN_OK_IF_FAILED(hr);

  for (auto& method_replacement : method_replacements) {
    const auto& wrapper_method_key =
        method_replacement.wrapper_method.get_method_cache_key();
    mdMemberRef wrapper_method_ref = mdMemberRefNil;

    if (!module_metadata->TryGetWrapperMemberRef(wrapper_method_key,
                                                 wrapper_method_ref)) {
      // no method ref token found for wrapper method, we can't do the
      // replacement, this should never happen because we always try to
      // add the method ref in ModuleLoadFinished()
      // TODO: log this
      return S_OK;
    }

    // for each IL instruction
    for (ILInstr* pInstr = rewriter.GetILList()->m_pNext;
         pInstr != rewriter.GetILList(); pInstr = pInstr->m_pNext) {
      // only CALL or CALLVIRT
      if (pInstr->m_opcode != CEE_CALL && pInstr->m_opcode != CEE_CALLVIRT) {
        continue;
      }

      // get the target function info, continue if its invalid
      auto target =
          GetFunctionInfo(module_metadata->metadata_import, pInstr->m_Arg32);
      if (!target.IsValid()) {
        continue;
      }

      // if the target matches by type name and method name
      if (method_replacement.target_method.type_name == target.type.name &&
          method_replacement.target_method.method_name == target.name) {
        // replace with a call to the instrumentation wrapper
        pInstr->m_opcode = CEE_CALL;
        pInstr->m_Arg32 = wrapper_method_ref;

        modified = true;
      }
    }

    if (modified) {
      LOG_APPEND(L"JITCompilationStarted(): Replaced calls from "
                 << caller.type.name << "." << caller.name << "() to "
                 << method_replacement.target_method.type_name << "."
                 << method_replacement.target_method.method_name
                 << "() with calls to "
                 << method_replacement.wrapper_method.type_name << "."
                 << method_replacement.wrapper_method.method_name << "().");
    }
  }

  if (modified) {
    hr = rewriter.Export();
    RETURN_OK_IF_FAILED(hr);
  }

  return S_OK;
}  // namespace trace

bool CorProfiler::IsAttached() const { return is_attached_; }

}  // namespace trace
