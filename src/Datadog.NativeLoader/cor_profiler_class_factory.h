// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.

#pragma once
#include "unknwn.h"
#include <atomic>

namespace datadog::nativeloader
{
class DynamicDispatcher;
} // namespace datadog::nativeloader

class CorProfilerClassFactory : public IClassFactory
{
private:
    std::atomic<int> m_refCount;
    datadog::nativeloader::DynamicDispatcher* m_dispatcher;

public:
    CorProfilerClassFactory(datadog::nativeloader::DynamicDispatcher* dispatcher);
    virtual ~CorProfilerClassFactory();
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override;
    ULONG STDMETHODCALLTYPE AddRef(void) override;
    ULONG STDMETHODCALLTYPE Release(void) override;
    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject) override;
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override;
};