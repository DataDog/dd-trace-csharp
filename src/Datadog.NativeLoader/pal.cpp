#include "pal.h"

#if _WIN32
#include <Windows.h>
#else
#include "dlfcn.h"
#endif

#include "logging.h"

namespace datadog
{
namespace nativeloader
{

    void* LoadDynamicLibrary(std::string filePath)
    {
        Debug("LoadLibrary: ", filePath);

#if _WIN32
        return LoadLibrary(ToWSTRING(filePath).c_str());
#else
        void* dynLibPtr = dlopen(filePath.c_str(), RTLD_LOCAL | RTLD_LAZY);
        if (dynLibPtr == nullptr)
        {
            char* errorMessage = dlerror();
            Warn("Error loading dynamic library: ", errorMessage);
        }
        return dynLibPtr;
#endif
    }

    void* GetExternalFunction(void* instance, std::string funcName)
    {
        Debug("GetExternalFunction: ", funcName);

#if _WIN32
        return (void*) GetProcAddress((HMODULE) instance, funcName.c_str());
#else
        void* dynFunc = dlsym(instance, funcName.c_str());
        if (dynFunc == nullptr)
        {
            char* errorMessage = dlerror();
            Warn("Error loading dynamic function: ", errorMessage);
        }
        return dynFunc;
#endif
    }

    bool FreeDynamicLibrary(void* handle)
    {
        Debug("FreeDynamicLibrary.");

#if _WIN32
        return FreeLibrary((HMODULE)handle);
#else
        return dlclose(handle) == 0;
#endif
    }

    WSTRING GetEnvironmentValue(const WSTRING& name)
    {
        Debug("GetEnvironmentValue: ", name);

#ifdef _WIN32
        const size_t max_buf_size = 4096;
        WSTRING buf(max_buf_size, 0);
        auto len = GetEnvironmentVariable(name.data(), buf.data(), (DWORD)(buf.size()));
        return Trim(buf.substr(0, len));
#else
        auto cstr = std::getenv(ToString(name).c_str());
        if (cstr == nullptr)
        {
            return WStr("");
        }
        std::string str(cstr);
        auto wstr = ToWSTRING(str);
        return Trim(wstr);
#endif
    }

    bool SetEnvironmentValue(const WSTRING& name, const WSTRING& value)
    {
        Debug("SetEnvironmentValue: ", name, "=", value);

#ifdef _WIN32
        return SetEnvironmentVariable(Trim(name).c_str(), value.c_str());
#else
        return setenv(ToString(name).c_str(), ToString(value).c_str(), 1) == 1;
#endif
    }

    std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name, const wchar_t delim)
    {
        std::vector<WSTRING> values;
        for (auto s : Split(GetEnvironmentValue(name), delim))
        {
            s = Trim(s);
            if (!s.empty())
            {
                values.push_back(s);
            }
        }
        return values;
    }

    std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name)
    {
        return GetEnvironmentValues(name, L';');
    }

} // namespace nativeloader
} // namespace datadog
