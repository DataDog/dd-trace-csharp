using System.Threading;
using Datadog.Trace.Ci;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing
{
    internal static class Common
    {
        static Common()
        {
            var settings = TracerSettings.FromDefaultSources();
            settings.TraceBufferSize = 1024 * 1024 * 45; // slightly lower than the 50mb payload agent limit.

            TestTracer = new Tracer(settings);
            ServiceName = TestTracer.DefaultServiceName;

            Tracer.Instance = TestTracer;

            // Preload environment variables.
            CIEnvironmentValues.DecorateSpan(null);
        }

        internal static Tracer TestTracer { get; private set; }

        internal static string ServiceName { get; private set; }

        internal static void FlushSpans(IntegrationInfo integrationInfo)
        {
            if (TestTracer.Settings.IsIntegrationEnabled(integrationInfo))
            {
                FlushSpans();
            }
        }

        internal static void FlushSpans()
        {
            SynchronizationContext context = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                // We have to ensure the flush of the buffer after we finish the tests of an assembly.
                // For some reason, sometimes when all test are finished none of the callbacks to handling the tracer disposal is triggered.
                // So the last spans in buffer aren't send to the agent.
                TestTracer.FlushAsync().GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(context);
            }
        }
    }
}
