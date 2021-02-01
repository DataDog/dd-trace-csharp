using System;
using System.Threading;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// NUnit.VisualStudio.TestAdapter.NUnitTestAdapter.Unload() calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "NUnit3.TestAdapter",
        TypeName = "NUnit.VisualStudio.TestAdapter.NUnitTestAdapter",
        MethodName = "Unload",
        ReturnTypeName = ClrNames.Void,
        MinimumVersion = "3.0.0",
        MaximumVersion = "3.*.*",
        IntegrationName = IntegrationName)]
    public class NUnitTestAdapterUnloadIntegration
    {
        private const string IntegrationName = nameof(IntegrationIds.NUnit);
        private static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>Return value of the method</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            if (Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId))
            {
                SynchronizationContext context = SynchronizationContext.Current;
                try
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                    // We have to ensure the flush of the buffer after we finish the tests of an assembly.
                    // For some reason, sometimes when all test are finished none of the callbacks to handling the tracer disposal is triggered.
                    // So the last spans in buffer aren't send to the agent.
                    // Other times we reach the 500 items of the buffer in a sec and the tracer start to drop spans.
                    // In a test scenario we must keep all spans.
                    Tracer.Instance.FlushAsync().GetAwaiter().GetResult();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(context);
                }
            }

            return CallTargetReturn.GetDefault();
        }
    }
}
