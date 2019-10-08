using System;
using System.Diagnostics;
using System.IO;
using Datadog.Trace.Vendors.Serilog;
using Datadog.Trace.Vendors.Serilog.Events;
using Datadog.Trace.Vendors.Serilog.Sinks.File;

namespace Datadog.Trace.Logging
{
    internal static class DatadogLogging
    {
        private const string NixDefaultDirectory = "/var/log/datadog/";
        private static readonly string WindowsDefaultDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Datadog .NET Tracer", "logs");

        private static readonly long? MaxLogFileSize = 10 * 1024 * 1024;
        private static readonly LogEventLevel MinimumLogEventLevel = LogEventLevel.Verbose; // Lowest level

        private static readonly ILogger SharedLogger = null;

        static DatadogLogging()
        {
            // No-op for if we fail to construct the file logger
            SharedLogger =
                new LoggerConfiguration()
                   .WriteTo.Sink<NullSink>()
                   .CreateLogger();
            try
            {
                var currentAppDomain = AppDomain.CurrentDomain;
                var currentProcess = Process.GetCurrentProcess();

                var debugEnabledVariable = Environment.GetEnvironmentVariable("DD_TRACE_DEBUG")?.ToLower();
                if (debugEnabledVariable != "1" && debugEnabledVariable != "true")
                {
                    // No verbose or debug logs
                    MinimumLogEventLevel = LogEventLevel.Information;
                }

                var maxLogSizeVar = Environment.GetEnvironmentVariable("DD_MAX_LOGFILE_SIZE");
                if (long.TryParse(maxLogSizeVar, out var maxLogSize))
                {
                    // No verbose or debug logs
                    MaxLogFileSize = maxLogSize;
                }

                var nativeLogFile = Environment.GetEnvironmentVariable("DD_TRACE_LOG_PATH");
                string logDirectory = null;

                if (!string.IsNullOrEmpty(nativeLogFile))
                {
                    logDirectory = Path.GetDirectoryName(nativeLogFile);
                }

                if (logDirectory == null)
                {
                    if (Directory.Exists(WindowsDefaultDirectory))
                    {
                        logDirectory = WindowsDefaultDirectory;
                    }
                    else if (Directory.Exists(NixDefaultDirectory))
                    {
                        logDirectory = NixDefaultDirectory;
                    }
                    else
                    {
                        logDirectory = Environment.CurrentDirectory;
                    }
                }

                // Ends in a dash because of the date postfix
                var managedLogPath = Path.Combine(logDirectory, $"dotnet-tracer-{currentProcess.ProcessName}-.log");

                var loggerConfiguration =
                    new LoggerConfiguration()
                       .Enrich.FromLogContext()
                       .MinimumLevel.Is(MinimumLogEventLevel)
                       .WriteTo.File(
                            managedLogPath,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{Properties}{NewLine}",
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: MaxLogFileSize);

                try
                {
                    loggerConfiguration.Enrich.WithProperty("MachineName", currentProcess.MachineName);
                    loggerConfiguration.Enrich.WithProperty("ProcessName", currentProcess.ProcessName);
                    loggerConfiguration.Enrich.WithProperty("PID", currentProcess.Id);
                    loggerConfiguration.Enrich.WithProperty("AppDomainName", currentAppDomain.FriendlyName);
                }
                catch
                {
                    // At all costs, make sure the logger works when possible.
                }

                SharedLogger = loggerConfiguration.CreateLogger();
            }
            catch
            {
                // nothing to do here
            }
        }

        public static ILogger GetLogger(Type classType)
        {
            // Tells us which types are loaded, when, and how often.
            SharedLogger.Information($"Logger retrieved for: {classType.AssemblyQualifiedName}");
            return SharedLogger;
        }

        public static ILogger For<T>()
        {
            return GetLogger(typeof(T));
        }
    }
}
