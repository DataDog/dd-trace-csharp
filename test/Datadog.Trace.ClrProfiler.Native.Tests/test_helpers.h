
#pragma once
#include "../../src/Datadog.Trace.ClrProfiler.Native/com_ptr.h"
#include "../../src/Datadog.Trace.ClrProfiler.Native/integration.h"

namespace trace {

class CLRHelperTestBase : public ::testing::Test {
 protected:
  IMetaDataDispenser* metadata_dispenser_;
  ComPtr<IMetaDataImport2> metadata_import_;
  ComPtr<IMetaDataAssemblyImport> assembly_import_;
  Version min_ver_ = Version(0, 0, 0, 0);
  Version max_ver_ = Version(USHRT_MAX, USHRT_MAX, USHRT_MAX, USHRT_MAX);

  void LoadMetadataDependencies() {
    ICLRMetaHost* metahost = nullptr;
    HRESULT hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost,
                                   (void**)&metahost);
    ASSERT_TRUE(SUCCEEDED(hr));

    IEnumUnknown* runtimes = nullptr;
    hr = metahost->EnumerateInstalledRuntimes(&runtimes);
    ASSERT_TRUE(SUCCEEDED(hr));

    ICLRRuntimeInfo* latest = nullptr;
    ICLRRuntimeInfo* runtime = nullptr;
    ULONG fetched = 0;
    while ((hr = runtimes->Next(1, (IUnknown**)&runtime, &fetched)) == S_OK &&
           fetched > 0) {
      latest = runtime;
    }

    hr =
        latest->GetInterface(CLSID_CorMetaDataDispenser, IID_IMetaDataDispenser,
                             (void**)&metadata_dispenser_);
    ASSERT_TRUE(SUCCEEDED(hr));

    ComPtr<IUnknown> metadataInterfaces;
    hr = metadata_dispenser_->OpenScope(L"Samples.ExampleLibrary.dll",
                                        ofReadWriteMask, IID_IMetaDataImport2,
                                        metadataInterfaces.GetAddressOf());
    ASSERT_TRUE(SUCCEEDED(hr)) << "Samples.ExampleLibrary.dll was not found.";

    metadata_import_ =
        metadataInterfaces.As<IMetaDataImport2>(IID_IMetaDataImport2);
    assembly_import_ = metadataInterfaces.As<IMetaDataAssemblyImport>(
        IID_IMetaDataAssemblyImport);
  }

  void SetUp() override { LoadMetadataDependencies(); }

  FunctionInfo FunctionToTest(WSTRING& type_name, WSTRING& method_name) const {
    for (auto& type_def : EnumTypeDefs(metadata_import_)) {
      for (auto& method_def : EnumMethods(metadata_import_, type_def)) {
        auto target = GetFunctionInfo(metadata_import_, method_def);
        if (target.type.name != type_name) {
          continue;
        }
        if (target.name != method_name) {
          continue;
        }
        return target;
      }
    }

    return {};
  }
};
}  // namespace trace
