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
            if (Common.TestTracer.Settings.IsIntegrationEnabled(IntegrationId))
            {
                NUnitIntegration.FlushSpans();
            }

            return CallTargetReturn.GetDefault();
        }
    }
}
