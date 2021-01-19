using System;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.XUnit
{
    /// <summary>
    /// Xunit.Sdk.TestInvoker`1.RunAsync calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        Assemblies = new[] { "xunit.execution.dotnet", "xunit.execution.desktop" },
        Type = "Xunit.Sdk.TestInvoker`1",
        Method = "RunAsync",
        ReturnTypeName = "System.Threading.Tasks.Task`1<System.Decimal>",
        ParametersTypesNames = new string[0],
        MinimumVersion = "2.2.0",
        MaximumVersion = "2.*.*",
        IntegrationName = IntegrationName)]
    public static class XUnitTestInvokerRunAsyncIntegration
    {
        private const string IntegrationName = "XUnit";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationName))
            {
                return CallTargetState.GetDefault();
            }

            TestInvokerStruct invokerInstance = instance.As<TestInvokerStruct>();
            TestRunnerStruct runnerInstance = new TestRunnerStruct
            {
                Aggregator = invokerInstance.Aggregator,
                TestCase = invokerInstance.TestCase,
                TestClass = invokerInstance.TestClass,
                TestMethod = invokerInstance.TestMethod,
                TestMethodArguments = invokerInstance.TestMethodArguments
            };

            return new CallTargetState(XUnitIntegration.CreateScope(ref runnerInstance, instance.GetType()));
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Task of HttpResponse message instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            Scope scope = (Scope)state.State;
            if (scope != null)
            {
                // Before returning the control flow we need to restore the parent Scope setted by ScopeFactory.CreateOutboundHttpScope
                // This doesn't affect to OnAsyncMethodEnd async continuation, an ExecutionContext is captured
                // by the inner await.
                IScopeManager scopeManager = ((IDatadogTracer)Tracer.Instance).ScopeManager;
                if (scopeManager.Active == scope)
                {
                    scopeManager.Close(scope);
                }
            }

            return new CallTargetReturn<TReturn>(returnValue);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static decimal OnAsyncMethodEnd<TTarget>(TTarget instance, decimal returnValue, Exception exception, CallTargetState state)
        {
            Scope scope = (Scope)state.State;
            if (scope != null)
            {
                TestInvokerStruct invokerInstance = instance.As<TestInvokerStruct>();
                XUnitIntegration.FinishScope(scope, invokerInstance.Aggregator);
            }

            return returnValue;
        }
    }
}
