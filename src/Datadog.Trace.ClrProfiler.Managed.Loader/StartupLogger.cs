using System;
using System.IO;

namespace Datadog.Trace.ClrProfiler.Managed.Loader
{
    internal static class StartupLogger
    {
        private const string NixDefaultDirectory = "/var/log/datadog/dotnet";

        private static readonly string LogDirectory = GetLogDirectory();
        private static readonly string StartupLogFilePath = LogDirectory != null ? Path.Combine(LogDirectory, "dotnet-tracer-loader.log") : null;

        public static bool IsWindows
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return (p == 0) || (p == 1) || (p == 2) || (p == 3);
            }
        }

        public static void Log(string message, params object[] args)
        {
            try
            {
                if (StartupLogFilePath != null)
                {
                    try
                    {
                        using (var fileSink = new FileSink(StartupLogFilePath))
                        {
                            fileSink.Info($"[{DateTime.UtcNow}] {message}{Environment.NewLine}", args);
                        }

                        return;
                    }
                    catch
                    {
                        // ignore
                    }
                }

                Console.Error.WriteLine(message, args);
            }
            catch
            {
                // ignore
            }
        }

        public static void Log(Exception ex, string message, params object[] args)
        {
            message = $"{message}{Environment.NewLine}{ex}";
            Log(message, args);
        }

        private static string GetLogDirectory()
        {
            string logDirectory = null;

            try
            {
                var nativeLogFile = Environment.GetEnvironmentVariable("DD_TRACE_LOG_PATH");

                if (!string.IsNullOrEmpty(nativeLogFile))
                {
                    logDirectory = Path.GetDirectoryName(nativeLogFile);
                }

                if (logDirectory == null)
                {
                    if (IsWindows)
                    {
                        var windowsDefaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Datadog .NET Tracer", "logs");
                        CreateDirectoryIfMissing(windowsDefaultDirectory, out logDirectory);
                    }
                    else
                    {
                        // either Linux or OS X
                        CreateDirectoryIfMissing(NixDefaultDirectory, out logDirectory);
                    }
                }

                if (logDirectory == null)
                {
                    // Last effort at writing logs
                    logDirectory = Path.GetTempPath();
                }
            }
            catch
            {
                // The try block may throw a SecurityException if not granted the System.Security.Permissions.FileIOPermission
                // because of the following API calls
                //   - Directory.Exists
                //   - Environment.GetFolderPath
                //   - Path.GetTempPath
            }

            return logDirectory;
        }

        private static void CreateDirectoryIfMissing(string pathToCreate, out string logDirectory)
        {
            if (Directory.Exists(pathToCreate))
            {
                logDirectory = pathToCreate;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(pathToCreate);
                    logDirectory = pathToCreate;
                }
                catch
                {
                    // Unable to create the directory meaning that the user
                    // will have to create it on their own.
                    // It is unsafe to log, so clear the out param
                    logDirectory = null;
                }
            }
        }
    }
}
