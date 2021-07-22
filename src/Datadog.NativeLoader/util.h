#pragma once
#include <filesystem>

#include "pal.h"
#include "string.h"

using namespace datadog::nativeloader;

const std::string conf_filename = "loader.conf";
const WSTRING cfg_filepath_env = WStr("DD_PROXY_CONFIGFILE");

// Gets the profiler path
static WSTRING GetProfilerPath()
{
    WSTRING profiler_path;

#if BIT64
    profiler_path = GetEnvironmentValue(WStr("CORECLR_PROFILER_PATH_64"));
    if (profiler_path.length() == 0)
    {
        profiler_path = GetEnvironmentValue(WStr("COR_PROFILER_PATH_64"));
    }
#else
    profiler_path = GetEnvironmentValue(WStr("CORECLR_PROFILER_PATH_32"));
    if (profiler_path.length() == 0)
    {
        profiler_path = GetEnvironmentValue(WStr("COR_PROFILER_PATH_32"));
    }
#endif

    if (profiler_path.length() == 0)
    {
        profiler_path = GetEnvironmentValue(WStr("CORECLR_PROFILER_PATH"));
    }
    if (profiler_path.length() == 0)
    {
        profiler_path = GetEnvironmentValue(WStr("COR_PROFILER_PATH"));
    }

    return profiler_path;
}

// Gets the configuration file path
static std::string GetConfigurationFilePath()
{
    WSTRING env_configfile = GetEnvironmentValue(cfg_filepath_env);
    if (env_configfile != WStr(""))
    {
        return ToString(env_configfile);
    }
    else
    {
        std::string profilerFilePath = ToString(GetProfilerPath());
        std::filesystem::path profilerPath = std::filesystem::path(profilerFilePath).remove_filename();
        return profilerPath.append(conf_filename).string();
    }
}