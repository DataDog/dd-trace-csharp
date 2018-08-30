#ifndef DD_CLR_PROFILER_PROFILER_H_
#define DD_CLR_PROFILER_PROFILER_H_

#include <vector>

#include <corhlpr.h>
#include <corprof.h>
#include "IDToInfoMap.h"
#include "ModuleMetadata.h"
#include "integration.h"
#include "profiler_base.h"

namespace trace {

class Profiler : public ProfilerBase {
 private:
  bool is_attached_ = false;
  IDToInfoMap<ModuleID, ModuleMetadata*> module_id_to_info_map_;
  const std::vector<integration> integrations_;

 public:
  Profiler();

  bool IsAttached() const;

  HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
  HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId,
                                                  BOOL fIsSafeToBlock) override;
  HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId,
                                               HRESULT hrStatus) override;
  HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(ModuleID moduleId,
                                                 HRESULT hrStatus) override;
};

// Note: Generally you should not have a single, global callback implementation,
// as that prevents your profiler from analyzing multiply loaded in-process
// side-by-side CLRs. However, this profiler implements the "profile-first"
// alternative of dealing with multiple in-process side-by-side CLR instances.
// First CLR to try to load us into this process wins; so there can only be one
// callback implementation created. (See ProfilerCallback::CreateObject.)
extern Profiler* profiler;  // global reference to callback object

}  // namespace trace

#endif  // DD_CLR_PROFILER_PROFILER_H_