namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class BuildParameters
    {
#if DEBUG
        public const string Configuration = "Debug";
#else
        public const string Configuration = "Release";
#endif

#if NETFRAMEWORK
        public const bool CoreClr = false;
#else
        public const bool CoreClr = true;
#endif

#if NET452
        public const string TargetFramework = "net452";
#elif NET461
        public const string TargetFramework = "net461";
#elif NETCOREAPP2_1
        public const string TargetFramework = "netcoreapp2.1";
#endif
    }
}
