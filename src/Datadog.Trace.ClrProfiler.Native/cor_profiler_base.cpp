﻿#include "cor_profiler_base.h"

namespace trace {

CorProfilerBase::CorProfilerBase() : refCount(0), info_(nullptr) {}

CorProfilerBase::~CorProfilerBase() {
  if (this->info_ != nullptr) {
    this->info_->Release();
    this->info_ = nullptr;
  }
}

HRESULT CorProfilerBase::QueryInterface(REFIID riid, void** ppvObject) {
  if (riid == __uuidof(ICorProfilerCallback8) ||
      riid == __uuidof(ICorProfilerCallback7) ||
      riid == __uuidof(ICorProfilerCallback6) ||
      riid == __uuidof(ICorProfilerCallback5) ||
      riid == __uuidof(ICorProfilerCallback4) ||
      riid == __uuidof(ICorProfilerCallback3) ||
      riid == __uuidof(ICorProfilerCallback2) ||
      riid == __uuidof(ICorProfilerCallback) || riid == IID_IUnknown) {
    *ppvObject = this;
    this->AddRef();
    return S_OK;
  }

  *ppvObject = nullptr;
  return E_NOINTERFACE;
}

ULONG CorProfilerBase::AddRef() {
  return std::atomic_fetch_add(&this->refCount, 1) + 1;
}

ULONG CorProfilerBase::Release() {
  const int count = std::atomic_fetch_sub(&this->refCount, 1) - 1;

  if (count <= 0) {
    delete this;
  }

  return count;
}

HRESULT STDMETHODCALLTYPE CorProfilerBase::Shutdown() {
  if (this->info_ != nullptr) {
    this->info_->Release();
    this->info_ = nullptr;
  }

  return S_OK;
}

}  // namespace trace
