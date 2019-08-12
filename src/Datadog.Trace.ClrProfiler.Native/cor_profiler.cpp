#include "cor_profiler.h"

#include <corprof.h>
#include <string>
#include "corhlpr.h"

#include "clr_helpers.h"
#include "dllmain.h"
#include "environment_variables.h"
#include "il_rewriter.h"
#include "il_rewriter_wrapper.h"
#include "integration_loader.h"
#include "logging.h"
#include "metadata_builder.h"
#include "module_metadata.h"
#include "pal.h"
#include "resource.h"
#include "util.h"

namespace trace {

CorProfiler* profiler = nullptr;

//
// ICorProfilerCallback methods
//
HRESULT STDMETHODCALLTYPE
CorProfiler::Initialize(IUnknown* cor_profiler_info_unknown) {
  // check if debug mode is enabled
  const auto debug_enabled_value =
      GetEnvironmentValue(environment::debug_enabled);

  if (debug_enabled_value == "1"_W || debug_enabled_value == "true"_W) {
    debug_logging_enabled = true;
  }

  CorProfilerBase::Initialize(cor_profiler_info_unknown);

  // check if tracing is completely disabled
  const WSTRING tracing_enabled =
      GetEnvironmentValue(environment::tracing_enabled);

  if (tracing_enabled == "0"_W || tracing_enabled == "false"_W) {
    Info("Profiler disabled in ", environment::tracing_enabled);
    return E_FAIL;
  }

  const auto process_name = GetCurrentProcessName();
  const auto include_process_names =
      GetEnvironmentValues(environment::include_process_names);

  // if there is a process inclusion list, attach profiler only if this
  // process's name is on the list
  if (!include_process_names.empty() &&
      !Contains(include_process_names, process_name)) {
    Info("Profiler disabled: ", process_name, " not found in ",
         environment::include_process_names, ".");
    return E_FAIL;
  }

  const auto exclude_process_names =
      GetEnvironmentValues(environment::exclude_process_names);

  // attach profiler only if this process's name is NOT on the list
  if (Contains(exclude_process_names, process_name)) {
    Info("Profiler disabled: ", process_name, " found in ",
         environment::exclude_process_names, ".");
    return E_FAIL;
  }

  // get Profiler interface
  HRESULT hr = cor_profiler_info_unknown->QueryInterface<ICorProfilerInfo3>(
      &this->info_);
  if (FAILED(hr)) {
    Warn("Failed to attach profiler: interface ICorProfilerInfo3 not found.");
    return E_FAIL;
  }

  Info("Environment variables:");

  WSTRING env_vars[]{environment::tracing_enabled,
                     environment::debug_enabled,
                     environment::integrations_path,
                     environment::include_process_names,
                     environment::exclude_process_names,
                     environment::agent_host,
                     environment::agent_port,
                     environment::env,
                     environment::service_name,
                     environment::disabled_integrations,
                     environment::clr_disable_optimizations};

  for (auto&& env_var : env_vars) {
    Info("  ", env_var, "=", GetEnvironmentValue(env_var));
  }

  // get path to integration definition JSON files
  const WSTRING integrations_paths =
      GetEnvironmentValue(environment::integrations_path);

  if (integrations_paths.empty()) {
    Warn("Profiler disabled: ", environment::integrations_path,
         " environment variable not set.");
    return E_FAIL;
  }

  // load all available integrations from JSON files
  const std::vector<Integration> all_integrations =
      LoadIntegrationsFromEnvironment();

  // get list of disabled integration names
  const std::vector<WSTRING> disabled_integration_names =
      GetEnvironmentValues(environment::disabled_integrations);

  // remove disabled integrations
  integrations_ =
      FilterIntegrationsByName(all_integrations, disabled_integration_names);

  // check if there are any enabled integrations left
  if (integrations_.empty()) {
    Warn("Profiler disabled: no enabled integrations found.");
    return E_FAIL;
  }

  DWORD event_mask = COR_PRF_MONITOR_JIT_COMPILATION |
                     COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                     COR_PRF_DISABLE_INLINING | COR_PRF_MONITOR_MODULE_LOADS |
                     COR_PRF_MONITOR_ASSEMBLY_LOADS |
                     COR_PRF_DISABLE_ALL_NGEN_IMAGES;

  if (DisableOptimizations()) {
    Info("Disabling all code optimizations.");
    event_mask |= COR_PRF_DISABLE_OPTIMIZATIONS;
  }

  // set event mask to subscribe to events and disable NGEN images
  // get ICorProfilerInfo5 for net452+
  ICorProfilerInfo5* info5;
  hr = cor_profiler_info_unknown->QueryInterface<ICorProfilerInfo5>(&info5);
  if (SUCCEEDED(hr)) {
    Info("Interface ICorProfilerInfo5 found.");
    hr = info5->SetEventMask2(event_mask, COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES);
  } else {
    hr = this->info_->SetEventMask(event_mask);
  }

  if (FAILED(hr)) {
    Warn("Failed to attach profiler: unable to set event mask.");
    return E_FAIL;
  }

  // we're in!
  Info("Profiler attached.");
  this->info_->AddRef();
  is_attached_ = true;
  profiler = this;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE
CorProfiler::AssemblyLoadFinished(AssemblyID assembly_id, HRESULT hr_status) {
  if (FAILED(hr_status)) {
    // if assembly failed to load, skip it entirely,
    // otherwise we can crash the process if assembly is not valid
    CorProfilerBase::AssemblyLoadFinished(assembly_id, hr_status);
    return S_OK;
  }

  if (!is_attached_) {
    return S_OK;
  }

  const auto assembly_info = GetAssemblyInfo(this->info_, assembly_id);

  Info("AssemblyLoadFinished: hr_status=", hr_status,
       " assemblyID=", assembly_id, " assemblyName=", assembly_info.name,
       " appDomainID=", assembly_info.app_domain_id,
       " appDomainName=", assembly_info.app_domain_name);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassLoadFinished(ClassID class_id,
                                            HRESULT hr_status) {
  if (FAILED(hr_status)) {
    // if assembly failed to load, skip it entirely,
    // otherwise we can crash the process if assembly is not valid
    CorProfilerBase::ClassLoadFinished(class_id, hr_status);
    return S_OK;
  }

  if (!is_attached_) {
    return S_OK;
  }

  ModuleID module_id;
  mdTypeDef type_def_token;
  auto hr = this->info_->GetClassIDInfo(class_id, &module_id, &type_def_token);
  if (FAILED(hr)) {
    Info("ClassLoadFinished: Failed to get information on classID=", class_id);
    return S_OK;
  }

  const auto module_info = GetModuleInfo(this->info_, module_id);
  ComPtr<IUnknown> metadata_interfaces;
  hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite,
                                           IID_IMetaDataImport2,
                                           metadata_interfaces.GetAddressOf());

  if (FAILED(hr)) {
    Warn("ClassLoadFinished failed to get metadata interface for ", module_id,
         " ", module_info.assembly.name);
    return S_OK;
  }

  const auto metadata_import =
      metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
  const auto metadata_emit =
      metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
  const auto assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(
      IID_IMetaDataAssemblyImport);
  const auto assembly_emit =
      metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

  auto typeinfo = GetTypeInfo(metadata_import, type_def_token);

  Info("ClassLoadFinished: hr_status=", hr_status, " classID=", class_id,
       " TypeDefToken=", type_def_token, " Name=", typeinfo.name);
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadFinished(ModuleID module_id,
                                                          HRESULT hr_status) {
  if (FAILED(hr_status)) {
    // if module failed to load, skip it entirely,
    // otherwise we can crash the process if module is not valid
    CorProfilerBase::ModuleLoadFinished(module_id, hr_status);
    return S_OK;
  }

  if (!is_attached_) {
    return S_OK;
  }

  // keep this lock until we are done using the module,
  // to prevent it from unloading while in use
  std::lock_guard<std::mutex> guard(module_id_to_info_map_lock_);

  const auto module_info = GetModuleInfo(this->info_, module_id);
  if (!module_info.IsValid()) {
    return S_OK;
  }

  AppDomainID app_domain_id = module_info.assembly.app_domain_id;

  // Identify the AppDomain ID of mscorlib which will be the Shared Domain
  // because mscorlib is always a domain-neutral assembly
  if (!mscorlib_module_loaded && module_info.assembly.name == "mscorlib"_W) {
    mscorlib_module_loaded = true;
    mscorlib_app_domain_id = app_domain_id;
    return S_OK;
  }

  // Identify the AppDomain ID of the managed profiler entrypoint
  if (module_info.assembly.name == "Datadog.Trace.ClrProfiler.Managed"_W) {
    managed_profiler_module_loaded = true;
    managed_profiler_app_domain_id = app_domain_id;
  }

  // Do not modify the module if it has been loaded into the Shared Domain
  // and the profiler is not in the Shared Domain
  if (mscorlib_module_loaded && managed_profiler_module_loaded &&
      app_domain_id == mscorlib_app_domain_id &&
      mscorlib_app_domain_id != managed_profiler_app_domain_id) {
    Info(
        "ModuleLoadFinished skipping modifying assembly because it is "
        "domain-neutral but the managed profiler is not: ",
        module_id, " ", module_info.assembly.name);
    return S_OK;
  }

  if (module_info.IsWindowsRuntime()) {
    // We cannot obtain writable metadata interfaces on Windows Runtime modules
    // or instrument their IL.
    Debug("ModuleLoadFinished skipping Windows Metadata module: ", module_id,
          " ", module_info.assembly.name);
    return S_OK;
  }

  // We must never try to add assembly references to
  // mscorlib or netstandard. Skip other known assemblies.
  WSTRING skip_assemblies[]{
      "mscorlib"_W,
      "netstandard"_W,
      "Datadog.Trace"_W,
      "Datadog.Trace.ClrProfiler.Managed"_W,
      "MsgPack"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers0"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers1"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers2"_W,
      "Sigil"_W,
      "Sigil.Emit.DynamicAssembly"_W,
      "System.Core"_W,
      "System.Runtime"_W,
      "System.IO.FileSystem"_W,
      "System.Collections"_W,
      "System.Runtime.Extensions"_W,
      "System.Threading.Tasks"_W,
      "System.Runtime.InteropServices"_W,
      "System.Runtime.InteropServices.RuntimeInformation"_W,
      "System.ComponentModel"_W,
      "System.Console"_W,
      "System.Diagnostics.DiagnosticSource"_W,
      "Microsoft.Extensions.Options"_W,
      "Microsoft.Extensions.ObjectPool"_W,
      "System.Configuration"_W,
      "System.Xml.Linq"_W,
      "Microsoft.AspNetCore.Razor.Language"_W,
      "Microsoft.AspNetCore.Mvc.RazorPages"_W,
      "Microsoft.CSharp"_W,
      "Newtonsoft.Json"_W,
      "Anonymously Hosted DynamicMethods Assembly"_W,
      "ISymWrapper"_W};

  for (auto&& skip_assembly : skip_assemblies) {
    if (module_info.assembly.name == skip_assembly) {
      Debug("ModuleLoadFinished skipping known module: ", module_id, " ",
            module_info.assembly.name);
      return S_OK;
    }
  }

  std::vector<IntegrationMethod> filtered_integrations =
      FlattenIntegrations(integrations_);

  filtered_integrations =
      FilterIntegrationsByCaller(filtered_integrations, module_info.assembly);
  if (filtered_integrations.empty()) {
    // we don't need to instrument anything in this module, skip it
    Debug("ModuleLoadFinished skipping module (filtered by caller): ",
          module_id, " ", module_info.assembly.name);
    return S_OK;
  }

  ComPtr<IUnknown> metadata_interfaces;

  auto hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite,
                                           IID_IMetaDataImport2,
                                           metadata_interfaces.GetAddressOf());

  if (FAILED(hr)) {
    Warn("ModuleLoadFinished failed to get metadata interface for ", module_id,
         " ", module_info.assembly.name);
    return S_OK;
  }

  const auto metadata_import =
      metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
  const auto metadata_emit =
      metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
  const auto assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(
      IID_IMetaDataAssemblyImport);
  const auto assembly_emit =
      metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

  filtered_integrations =
      FilterIntegrationsByTarget(filtered_integrations, assembly_import);
  if (filtered_integrations.empty()) {
    // we don't need to instrument anything in this module, skip it
    Debug("ModuleLoadFinished skipping module (filtered by target): ",
          module_id, " ", module_info.assembly.name);
    return S_OK;
  }

  mdModule module;
  hr = metadata_import->GetModuleFromScope(&module);
  if (FAILED(hr)) {
    Warn("ModuleLoadFinished failed to get module metadata token for ",
         module_id, " ", module_info.assembly.name);
    return S_OK;
  }

  ModuleMetadata* module_metadata =
      new ModuleMetadata(metadata_import, metadata_emit,
                         module_info.assembly.name, filtered_integrations);

  const MetadataBuilder metadata_builder(*module_metadata, module,
                                         metadata_import, metadata_emit,
                                         assembly_import, assembly_emit);

  for (const auto& integration : filtered_integrations) {
    // for each wrapper assembly, emit an assembly reference
    hr = metadata_builder.EmitAssemblyRef(
        integration.replacement.wrapper_method.assembly);
    if (FAILED(hr)) {
      Warn("ModuleLoadFinished failed to emit wrapper assembly ref for ",
           module_id, " ", module_info.assembly.name);
      return S_OK;
    }

    // for each method replacement in each enabled integration,
    // emit a reference to the instrumentation wrapper methods
    hr = metadata_builder.StoreWrapperMethodRef(integration.replacement);
    if (FAILED(hr)) {
      Warn("ModuleLoadFinished failed to emit or store wrapper method ref for ",
           module_id, " ", module_info.assembly.name);
      return S_OK;
    }
  }

  // store module info for later lookup
  module_id_to_info_map_[module_id] = module_metadata;

  Info("ModuleLoadFinished emitted new metadata into ", module_id, " ",
       module_info.assembly.name, " AppDomain ",
       module_info.assembly.app_domain_id, " ",
       module_info.assembly.app_domain_name, ". .");
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleUnloadStarted(ModuleID module_id) {
  if (debug_logging_enabled) {
    const auto module_info = GetModuleInfo(this->info_, module_id);

    if (module_info.IsValid()) {
      Debug("ModuleUnloadStarted: ", module_id, " ", module_info.assembly.name,
            " AppDomain ", module_info.assembly.app_domain_id, " ",
            module_info.assembly.app_domain_name);
    } else {
      Debug("ModuleUnloadStarted: ", module_id);
    }
  }

  // take this lock so we block until the
  // module metadata is not longer being used
  std::lock_guard<std::mutex> guard(module_id_to_info_map_lock_);

  // remove module metadata from map
  if (module_id_to_info_map_.count(module_id) > 0) {
    ModuleMetadata* metadata = module_id_to_info_map_[module_id];
    module_id_to_info_map_.erase(module_id);
    delete metadata;
  }

  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::Shutdown() {
  CorProfilerBase::Shutdown();

  // keep this lock until we are done using the module,
  // to prevent it from unloading while in use
  std::lock_guard<std::mutex> guard(module_id_to_info_map_lock_);

  is_attached_ = false;
  return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStarted(
    FunctionID function_id, BOOL is_safe_to_block) {
  if (!is_attached_ || !is_safe_to_block) {
    return S_OK;
  }

  // keep this lock until we are done using the module,
  // to prevent it from unloading while in use
  std::lock_guard<std::mutex> guard(module_id_to_info_map_lock_);

  ModuleID module_id;
  mdToken function_token = mdTokenNil;

  HRESULT hr = this->info_->GetFunctionInfo(function_id, nullptr, &module_id,
                                            &function_token);
  RETURN_OK_IF_FAILED(hr);

  ModuleMetadata* module_metadata = nullptr;

  if (module_id_to_info_map_.count(module_id) > 0) {
    module_metadata = module_id_to_info_map_[module_id];
  }

  if (module_metadata == nullptr) {
    // we haven't stored a ModuleMetadata for this module,
    // so we can't modify its IL
    return S_OK;
  }

  // get function info
  auto caller =
      GetFunctionInfo(module_metadata->metadata_import, function_token);
  if (!caller.IsValid()) {
    return S_OK;
  }

  if (true) {
    Info("JITCompilationStarted: function_id=", function_id,
          " token=", function_token, " name=", caller.type.name, ".",
          caller.name, "()");
  }

  if (!first_jit_compilation_completed) {
    first_jit_compilation_completed = true;
    hr = TryLoadManagedCode(module_metadata->metadata_emit, module_id,
                            function_token);
    RETURN_OK_IF_FAILED(hr);
  }

  auto method_replacements =
      module_metadata->GetMethodReplacementsForCaller(caller);
  if (method_replacements.empty()) {
    return S_OK;
  }

  ILRewriter rewriter(this->info_, nullptr, module_id, function_token);
  bool modified = false;

  hr = rewriter.Import();

  // TODO: REMOVE
  // Let's print out the local variable token
  mdSignature localvarsig = rewriter.GetTkLocalVarSig();
  PCCOR_SIGNATURE rgbOrigSig = NULL;
  ULONG cbOrigSig;
  module_metadata->metadata_import->GetSigFromToken(localvarsig, &rgbOrigSig,
                                                    &cbOrigSig);
  //
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

      // make sure the type and method names match
      if (method_replacement.target_method.type_name != target.type.name ||
          method_replacement.target_method.method_name != target.name) {
        continue;
      }

      auto wrapper_method_signature_size =
          method_replacement.wrapper_method.method_signature.data.size();

      if (wrapper_method_signature_size < 5) {
        // This is invalid, we should always have the wrapper fully defined
        // Minimum:
        // 0:{CallingConvention}|1:{ParamCount}|2:{ReturnType}|3:{OpCode}|4:{mdToken}
        // Drop out for safety
        if (debug_logging_enabled) {
          Debug(
              "JITCompilationStarted skipping method: signature too short. "
              "function_id=",
              function_id, " token=", function_token,
              " name=", caller.type.name, ".", caller.name, "()",
              " wrapper_method_signature_size=", wrapper_method_signature_size);
        }

        continue;
      }

      auto expected_number_args = method_replacement.wrapper_method
                                      .method_signature.NumberOfArguments();

      // We pass the opcode and mdToken as the last arguments to every wrapper
      // method
      expected_number_args = expected_number_args - 2;

      if (target.signature.IsInstanceMethod()) {
        // We always pass the instance as the first argument
        expected_number_args--;
      }

      auto target_arg_count = target.signature.NumberOfArguments();

      if (expected_number_args != target_arg_count) {
        // Number of arguments does not match our wrapper method
        if (debug_logging_enabled) {
          Debug(
              "JITCompilationStarted skipping method: argument counts don't "
              "match. function_id=",
              function_id, " token=", function_token,
              " name=", caller.type.name, ".", caller.name, "()",
              " expected_number_args=", expected_number_args,
              " target_arg_count=", target_arg_count);
        }

        continue;
      }

      auto method_def_md_token = target.id;

      if (target.is_generic) {
        if (target.signature.NumberOfTypeArguments() !=
            method_replacement.wrapper_method.method_signature
                .NumberOfTypeArguments()) {
          // Number of generic arguments does not match our wrapper method
          continue;
        }

        // we need to emit a method spec to populate the generic arguments
        wrapper_method_ref =
            DefineMethodSpec(module_metadata->metadata_emit, wrapper_method_ref,
                             target.function_spec_signature);
        method_def_md_token = target.method_def_id;
      }

      std::vector<WSTRING> sig_types;
      const auto successfully_parsed_signature = TryParseSignatureTypes(
          module_metadata->metadata_import, target, sig_types);
      auto expected_sig_types =
          method_replacement.target_method.signature_types;

      if (!successfully_parsed_signature) {
        if (debug_logging_enabled) {
          Debug(
              "JITCompilationStarted skipping method: failed to parse "
              "signature. function_id=",
              function_id, " token=", function_token,
              " name=", caller.type.name, ".", caller.name, "()",
              " successfully_parsed_signature=", successfully_parsed_signature,
              " sig_types.size()=", sig_types.size(),
              " expected_sig_types.size()=", expected_sig_types.size());
        }

        continue;
      }

      if (sig_types.size() != expected_sig_types.size()) {
        // we can't safely assume our wrapper methods handle the types
        if (debug_logging_enabled) {
          Debug(
              "JITCompilationStarted skipping method: unexpected type count. "
              "function_id=",
              function_id, " token=", function_token,
              " name=", caller.type.name, ".", caller.name, "()",
              " successfully_parsed_signature=", successfully_parsed_signature,
              " sig_types.size()=", sig_types.size(),
              " expected_sig_types.size()=", expected_sig_types.size());
        }

        continue;
      }

      auto is_match = true;
      for (size_t i = 0; i < expected_sig_types.size(); i++) {
        if (expected_sig_types[i] == "_"_W) {
          // We are supposed to ignore this index
          continue;
        }
        if (expected_sig_types[i] != sig_types[i]) {
          // we have a type mismatch, drop out
          if (debug_logging_enabled) {
            Debug(
                "JITCompilationStarted skipping method: types don't match. "
                "function_id=",
                function_id, " token=", function_token,
                " name=", caller.type.name, ".", caller.name, "()",
                " expected_sig_types[", i, "]=", expected_sig_types[i],
                " sig_types[", i, "]=", sig_types[i]);
          }

          is_match = false;
          break;
        }
      }

      if (!is_match) {
        // signatures don't match
        continue;
      }

      const auto original_argument = pInstr->m_Arg32;

      // insert the opcode and signature token as
      // additional arguments for the wrapper method
      ILRewriterWrapper rewriter_wrapper(&rewriter);
      rewriter_wrapper.SetILPosition(pInstr);
      rewriter_wrapper.LoadInt32(pInstr->m_opcode);
      rewriter_wrapper.LoadInt32(method_def_md_token);

      // always use CALL because the wrappers methods are all static
      pInstr->m_opcode = CEE_CALL;
      // replace with a call to the instrumentation wrapper
      pInstr->m_Arg32 = wrapper_method_ref;

      modified = true;

      Info("*** JITCompilationStarted() replaced calls from ", caller.type.name,
           ".", caller.name, "() to ",
           method_replacement.target_method.type_name, ".",
           method_replacement.target_method.method_name, "() ",
           original_argument, " with calls to ",
           method_replacement.wrapper_method.type_name, ".",
           method_replacement.wrapper_method.method_name, "() ",
           wrapper_method_ref);
    }
  }

  if (modified) {
    hr = rewriter.Export();
    RETURN_OK_IF_FAILED(hr);
  }

  return S_OK;
}

bool CorProfiler::IsAttached() const { return is_attached_; }

// Helper methods

HRESULT CorProfiler::TryLoadManagedCode(
    const ComPtr<IMetaDataEmit2>& metadata_emit, const ModuleID module_id,
    const mdToken function_token) {
  ILRewriter rewriter(this->info_, nullptr, module_id, function_token);

  mdMethodDef ret_method_token;
  auto hr = CreateVoidMethod(module_id, &ret_method_token);
  if (FAILED(hr)) {
    Warn("TryLoadManagedCode: Call to CreateVoidMethod(", module_id,
         ") failed");
    return S_OK;
  }

  hr = rewriter.Import();
  RETURN_OK_IF_FAILED(hr);

  ILRewriterWrapper rewriter_wrapper(&rewriter);

  // Get first instruction and set the rewriter to that location
  ILInstr* pInstr = rewriter.GetILList()->m_pNext;
  rewriter_wrapper.SetILPosition(pInstr);
  rewriter_wrapper.CallMember(ret_method_token, false);
  hr = rewriter.Export(); // TODO Uncomment the export call once I figure out the right signature token
  RETURN_OK_IF_FAILED(hr);

  metadata_emit->Save(
      L"C:\\Users\\zach.montoya\\dd\\dd-trace-dotnet\\inmemory.dll", 0);

  return S_OK;
}

HRESULT CorProfiler::CreateVoidMethod(const ModuleID module_id,
                                      mdMethodDef* ret_method_token) {
  ComPtr<IUnknown> metadata_interfaces;

  auto hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite,
                                           IID_IMetaDataImport2,
                                           metadata_interfaces.GetAddressOf());

  if (FAILED(hr)) {
    Warn("CreateVoidMethod: failed to get metadata interface for ", module_id);
    return S_OK;
  }

  const auto metadata_import =
      metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
  const auto metadata_emit =
      metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
  const auto assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(
      IID_IMetaDataAssemblyImport);
  const auto assembly_emit =
      metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

  mdTypeDef new_type_def;

  // Get reference to mscorlib so I can get a type ref for System.Object
  mdModuleRef mscorlib_ref;
  ASSEMBLYMETADATA metadata;
  ZeroMemory(&metadata, sizeof(metadata));
  metadata.usMajorVersion = 4;
  metadata.usMinorVersion = 0;
  metadata.usBuildNumber = 0;
  metadata.usRevisionNumber = 0;
  BYTE public_key[] = {0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89};
  assembly_emit->DefineAssemblyRef(public_key, sizeof(public_key), L"mscorlib",
                                   &metadata, NULL, 0, 0, &mscorlib_ref);

  // Create a new class that extends System.Object
  mdTypeRef object_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref, L"System.Object",
                                          &object_type_ref);
  if (FAILED(hr)) {
    Warn(
        "CreateVoidMethod: failed to create a TypeRef for System.Object for "
        "new emitted class, ",
        module_id);
    return S_OK;
  }

  metadata_emit->DefineTypeDef(L"__DDVoidMethodType__", tdAbstract | tdSealed,
                               object_type_ref, NULL, &new_type_def);
  BYTE initialize_signature[] = {0,  // IMAGE_CEE_CS_CALLCONV_DEFAULT
                                 0,
                                 ELEMENT_TYPE_VOID,  // ret = ELEMENT_TYPE_VOID
                                 ELEMENT_TYPE_OBJECT};

  metadata_emit->DefineMethod(
      new_type_def, L"__DDVoidMethodCall__", mdStatic, initialize_signature,
      sizeof(initialize_signature), 0, 0, ret_method_token);
  if (FAILED(hr)) {
    Warn(
        "CreateVoidMethod: failed to create a MethodDef for "
        "__DDVoidMethodType__.Initialize, ",
        module_id);
    return S_OK;
  }

  mdMethodDef consume_object_md;
  BYTE consume_object_signature[] = {
      0,  // IMAGE_CEE_CS_CALLCONV_DEFAULT
      1,
      ELEMENT_TYPE_VOID,  // ret = ELEMENT_TYPE_VOID
      ELEMENT_TYPE_OBJECT
  };

  metadata_emit->DefineMethod(new_type_def, L"__DDMethodCallConsumesObject__",
                              mdStatic, consume_object_signature,
                              sizeof(consume_object_signature), 0, 0,
                              &consume_object_md);

  COR_SIGNATURE get_assembly_bytes_signature[] = {
      IMAGE_CEE_CS_CALLCONV_DEFAULT,
      4,
      ELEMENT_TYPE_VOID,  // ret = ELEMENT_TYPE_VOID
      ELEMENT_TYPE_BYREF,
      ELEMENT_TYPE_I,
      ELEMENT_TYPE_BYREF,
      ELEMENT_TYPE_I4,
      ELEMENT_TYPE_BYREF,
      ELEMENT_TYPE_I,
      ELEMENT_TYPE_BYREF,
      ELEMENT_TYPE_I4,
  };

  mdModuleRef profiler_ref;
  hr = metadata_emit->DefineModuleRef(L"DATADOG.TRACE.CLRPROFILER.NATIVE.DLL",
                                      &profiler_ref);
  mdMethodDef pinvoke_method_def;
  metadata_emit->DefineMethod(
      new_type_def, L"GetAssemblyBytes", mdStatic | mdPinvokeImpl | mdHideBySig,
      get_assembly_bytes_signature, sizeof(get_assembly_bytes_signature), 0, 0,
      &pinvoke_method_def);

  metadata_emit->SetMethodImplFlags(pinvoke_method_def, miPreserveSig);

  mdParamDef data_param_def;
  hr = metadata_emit->DefineParam(pinvoke_method_def, 1, L"data", pdOut, 0,
                                  NULL, 0, &data_param_def);

  mdParamDef size_param_def;
  hr = metadata_emit->DefineParam(pinvoke_method_def, 2, L"size",
                                  pdOut | pdHasFieldMarshal, 0, NULL, 0,
                                  &size_param_def);

  hr = metadata_emit->DefinePinvokeMap(pinvoke_method_def,
                                       0,  // pmCallConvStdcall | pmNoMangle,
                                       L"GetAssemblyBytes", profiler_ref);

  // Helper routines to get metadata tokens
  // Get a TypeRef token for System.Byte
  mdTypeRef byte_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref, L"System.Byte",
                                          &byte_type_ref);
  if (FAILED(hr)) {
    Warn(
        "CreateVoidMethod: fail quickly",
        module_id);
    return S_OK;
  }

  // Get a MemberRef for System.Runtime.InteropSercies.Marshal.Copy(IntPtr, Byte[], int, int)
  mdTypeRef marshal_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref, L"System.Runtime.InteropServices.Marshal",
                                          &marshal_type_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  COR_SIGNATURE marshal_copy_signature[] = {
      IMAGE_CEE_CS_CALLCONV_DEFAULT,
      4,
      ELEMENT_TYPE_VOID,  // ret = void
      ELEMENT_TYPE_I,
      ELEMENT_TYPE_SZARRAY,
      ELEMENT_TYPE_U1,
      ELEMENT_TYPE_I4,
      ELEMENT_TYPE_I4
  };

  mdMemberRef marshal_copy_member_ref;
  hr = metadata_emit->DefineMemberRef(
      marshal_type_ref, L"Copy", marshal_copy_signature,
      sizeof(marshal_copy_signature), &marshal_copy_member_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  // Get a TypeRef for System.Reflection.Assembly
  mdTypeRef system_reflection_assembly_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref,
                                          L"System.Reflection.Assembly",
                                          &system_reflection_assembly_type_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  // Get a MemberRef for System.Object.ToString()
  mdTypeRef system_object_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref,
                                          L"System.Object",
                                          &system_object_type_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  COR_SIGNATURE object_tostring_signature[] = {
      IMAGE_CEE_CS_CALLCONV_HASTHIS,
      0,
      ELEMENT_TYPE_STRING,
  };

  mdMemberRef object_tostring_member_ref;
  hr = metadata_emit->DefineMemberRef(
      system_object_type_ref, L"ToString", object_tostring_signature,
                                      sizeof(object_tostring_signature), &object_tostring_member_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  // Get a MemberRef for System.Reflection.AppDomain.get_CurrentDomain()
  // and System.AppDomain.Assembly.Load(byte[], byte[])
  mdTypeRef system_appdomain_type_ref;
  hr = metadata_emit->DefineTypeRefByName(mscorlib_ref,
                                          L"System.AppDomain",
                                          &system_appdomain_type_ref);
  
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  COR_SIGNATURE system_reflection_assembly_type_ref_compressed;
  CorSigCompressToken(system_reflection_assembly_type_ref,
                      &system_reflection_assembly_type_ref_compressed);
  COR_SIGNATURE system_appdomain_type_ref_compressed;
  CorSigCompressToken(system_appdomain_type_ref,
                      &system_appdomain_type_ref_compressed);

  COR_SIGNATURE appdomain_get_current_domain_signature[] = {
      IMAGE_CEE_CS_CALLCONV_DEFAULT,
      0,
      ELEMENT_TYPE_CLASS, // ret = System.AppDomain
      system_appdomain_type_ref_compressed,
  };
  COR_SIGNATURE appdomain_load_signature[] = {
      IMAGE_CEE_CS_CALLCONV_HASTHIS,
      2,
      ELEMENT_TYPE_CLASS,  // ret = System.Reflection.Assembly
      system_reflection_assembly_type_ref_compressed,
      ELEMENT_TYPE_SZARRAY,
      ELEMENT_TYPE_U1,
      ELEMENT_TYPE_SZARRAY,
      ELEMENT_TYPE_U1,
  };
  COR_SIGNATURE assembly_create_instance_signature[] = {
      IMAGE_CEE_CS_CALLCONV_HASTHIS,
      1,
      ELEMENT_TYPE_OBJECT,  // ret = System.Object
      ELEMENT_TYPE_STRING
  };

  mdMemberRef appdomain_get_current_domain_member_ref;
  hr = metadata_emit->DefineMemberRef(
      system_appdomain_type_ref, L"get_CurrentDomain",
      appdomain_get_current_domain_signature,
      sizeof(appdomain_get_current_domain_signature),
      &appdomain_get_current_domain_member_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }
  mdMemberRef appdomain_load_member_ref;
  hr = metadata_emit->DefineMemberRef(
      system_appdomain_type_ref, L"Load", appdomain_load_signature,
      sizeof(appdomain_load_signature), &appdomain_load_member_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }
  mdMemberRef assembly_create_instance_member_ref;
  hr = metadata_emit->DefineMemberRef(
      system_reflection_assembly_type_ref, L"CreateInstance",
      assembly_create_instance_signature,
      sizeof(assembly_create_instance_signature),
      &assembly_create_instance_member_ref);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  // Create a string representing "Datadog.Trace.ClrProfiler.EntrypointManaged.LoadHelper"
  LPCWSTR load_helper_str =
      L"Datadog.Trace.ClrProfiler.EntrypointManaged.LoadHelper";
  DWORD load_helper_str_size = wcslen(load_helper_str);
  mdString load_helper_token;
  hr = metadata_emit->DefineUserString(load_helper_str, load_helper_str_size,
                                  &load_helper_token);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  DWORD string_len = 0;
  WCHAR string_contents[kNameMaxSize]{};
  hr = metadata_import->GetUserString(load_helper_token, string_contents,
                                      kNameMaxSize, &string_len);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  // Step 0) Define the locals so they appear in the following order:
  //         [0] System.IntPtr
  //         [1] System.Int32
  //         [2] System.IntPtr
  //         [3] System.Int32
  //         [4] System.Byte[]
  //         [5] System.Byte[]
  //         [6] class System.Reflection.Assembly
  // Generate a signature to describe the local variables
  mdSignature locals_signature_token;
  COR_SIGNATURE locals_signature[] = {
      IMAGE_CEE_CS_CALLCONV_LOCAL_SIG,
      7,
      ELEMENT_TYPE_I,
      ELEMENT_TYPE_I4,
      ELEMENT_TYPE_I,
      ELEMENT_TYPE_I4,
      ELEMENT_TYPE_SZARRAY,
      ELEMENT_TYPE_U1,
      ELEMENT_TYPE_SZARRAY,
      ELEMENT_TYPE_U1,
      ELEMENT_TYPE_CLASS,
      system_reflection_assembly_type_ref_compressed,
  };
  hr = metadata_emit->GetTokenFromSig(locals_signature, sizeof(locals_signature),
                                 &locals_signature_token);
  if (FAILED(hr)) {
    Warn("CreateVoidMethod: fail quickly", module_id);
    return S_OK;
  }

  /////////////////////////////////////////////
  // Define the method that just consumes the object
  ILRewriter rewriter_consumer(this->info_, nullptr, module_id, consume_object_md);
  rewriter_consumer.InitializeTiny();

  ILInstr* pFirstInstr = rewriter_consumer.GetILList()->m_pNext;
  ILInstr* pNewInstr = rewriter_consumer.NewILInstr();
  pNewInstr->m_opcode = CEE_RET;
  rewriter_consumer.InsertBefore(pFirstInstr, pNewInstr);

  hr = rewriter_consumer.Export();
  RETURN_OK_IF_FAILED(hr);
  /////////////////////////////////////////////

  /////////////////////////////////////////////
  // Now we need to add IL instructions into the void method
  ILRewriter rewriter_void(this->info_, nullptr, module_id, *ret_method_token);
  rewriter_void.InitializeTiny();
  rewriter_void.SetTkLocalVarSig(locals_signature_token);
  pFirstInstr = rewriter_void.GetILList()->m_pNext;

  // Step 1) Call GetAssemblyBytes(out IntPtr assemblyPtr, out int assemblySize, out IntPtr symbolsPtr, out int symbolsSize)

  // ldloca.s 0 : Load the address of the System.IntPtr local variable assemblyPtr
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOCA_S;
  pNewInstr->m_Arg32 = 0;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloca.s 1 : Load the address of the int local variable assemblySize
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOCA_S;
  pNewInstr->m_Arg32 = 1;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloca.s 2 : Load the address of the System.IntPtr local variable symbolsPtr
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOCA_S;
  pNewInstr->m_Arg32 = 2;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloca.s 3 : Load the address of the int local variable symbolsSize
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOCA_S;
  pNewInstr->m_Arg32 = 3;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // call void GetAssemblyBytes(native int&, int32&, natve int&, int32&)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALL;
  pNewInstr->m_Arg32 = pinvoke_method_def;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // Step 2) Call Marshal::Copy(IntPtr, byte[], int, int) to populate the assembly bytes

  // ldloc.1 : Load the assembly size variable (at index 1)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_1;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // newarr System.Byte : Create a new Byte[] to hold a managed copy of the assembly data
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_NEWARR;
  pNewInstr->m_Arg32 = byte_type_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // stloc.s 4 : Assign the Byte[] to the local variable at index 4
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_STLOC_S;
  pNewInstr->m_Arg8 = 4;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.0 : Load the value of the System.IntPtr local variable assemblyPtr (at index 0)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_0;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.s 4 : Load the value of the Byte[] local variable at index 4
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_S;
  pNewInstr->m_Arg8 = 4;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldc.i4.0 : Load the integer 0 for the Marshal.Copy startIndex parameter
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDC_I4_0;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.1 : Load the assembly size variable (at index 1) for the Marshal.Copy length parameter
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_1;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // call void [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, uint8[], int32, int32)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALL;
  pNewInstr->m_Arg32 = marshal_copy_member_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // Step 3) Call Marshal::Copy(IntPtr, byte[], int, int) to populate the symbols bytes

  // ldloc.3 : Load the symbols size variable (at index 3)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_3;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // newarr System.Byte : Create a new Byte[] to hold a managed copy of the assembly data
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_NEWARR;
  pNewInstr->m_Arg32 = byte_type_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // stloc.s 5 : Assign the Byte[] to the local variable at index 5
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_STLOC_S;
  pNewInstr->m_Arg8 = 5;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.2 : Load the value of the System.IntPtr local variable symbolsPtr (at index 2)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_2;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.s 5 : Load the value of the Byte[] local variable at index 5
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_S;
  pNewInstr->m_Arg8 = 5;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldc.i4.0 : Load the integer 0 for the Marshal.Copy startIndex parameter
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDC_I4_0;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.3 : Load the symbols size variable (at index 3) for the Marshal.Copy length parameter
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_3;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // call void [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, uint8[], int32, int32)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALL;
  pNewInstr->m_Arg32 = marshal_copy_member_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // Step 4) Call System.AppDomain.CurrentDomain.Load(byte[], byte[]))

  // call class [mscorlib]System.AppDomain [mscorlib]System.AppDomain::get_CurrentDomain()
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALL;
  pNewInstr->m_Arg32 = appdomain_get_current_domain_member_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.s 4 : Load the assembly byte array (at index 4) for the first byte[] parameter of AppDomain.Load(byte[], byte[])
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_S;
  pNewInstr->m_Arg8 = 4;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldloc.s 5 : Load the symbols byte array (at index 5) for the second byte[] parameter of AppDomain.Load(byte[], byte[])
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_S;
  pNewInstr->m_Arg8 = 5;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // callvirt instance class [mscorlib]System.Reflection.Assembly [mscorlib]System.AppDomain.Load(uint8[], uint8[])
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALLVIRT;
  pNewInstr->m_Arg32 = appdomain_load_member_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // stloc.s 6 : Assign the Assembly object to our local variable (at index 6)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_STLOC_S;
  pNewInstr->m_Arg8 = 6;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // Step 4) Call instance method Assembly.CreateInstance("Datadog.Trace.ClrProfiler.EntrypointManaged.LoadHelper.LoadManagedProfiler")

  // ldloc.s 6 : Load the Assembly object (at index 6) to call Assembly.CreateInstance
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDLOC_S;
  pNewInstr->m_Arg8 = 6;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // ldstr "Datadog.Trace.ClrProfiler.EntrypointManaged.LoadHelper"
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_LDSTR;
  pNewInstr->m_Arg32 = load_helper_token;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // callvirt instance object [mscorlib]System.Reflection.Assembly::CreateInstance(string)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALLVIRT;
  pNewInstr->m_Arg32 = assembly_create_instance_member_ref;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  // call instance void __DDMethodCallConsumesObject__(object)
  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_CALL;
  pNewInstr->m_Arg32 = consume_object_md;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
  //////////////////////////////////////////

  pNewInstr = rewriter_void.NewILInstr();
  pNewInstr->m_opcode = CEE_RET;
  rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

  hr = rewriter_void.Export();
  RETURN_OK_IF_FAILED(hr);

  return S_OK;
}

void CorProfiler::GetAssemblyBytes(BYTE** pAssemblyArray, int* assemblySize, BYTE** pSymbolsArray, int* symbolsSize) const {
  HINSTANCE hInstance = DllHandle;

  HRSRC hResAssemblyInfo =
      FindResource(hInstance, MAKEINTRESOURCE(MANAGED_ENTRYPOINT), L"ASSEMBLY");
  HGLOBAL hResAssembly = LoadResource(hInstance, hResAssemblyInfo);
  *assemblySize = SizeofResource(hInstance, hResAssemblyInfo);
  *pAssemblyArray = (LPBYTE)LockResource(hResAssembly);

  HRSRC hResSymbolsInfo =
      FindResource(hInstance, MAKEINTRESOURCE(MANAGED_ENTRYPOINT_SYMBOLS), L"SYMBOLS");
  HGLOBAL hResSymbols = LoadResource(hInstance, hResSymbolsInfo);
  *symbolsSize = SizeofResource(hInstance, hResSymbolsInfo);
  *pSymbolsArray = (LPBYTE)LockResource(hResSymbols);
  return;
}
}  // namespace trace
