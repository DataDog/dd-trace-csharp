// <copyright file="AzureFunctionInvokerInvokeAsyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Azure Function calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Microsoft.Azure.WebJobs.Host",
        TypeName = "Microsoft.Azure.WebJobs.Host.Executors.FunctionInvoker`2",
        MethodName = "InvokeAsync",
        ReturnTypeName = "System.Threading.Tasks.Task`1[System.Object]",
        ParameterTypeNames = new[] { "System.Object", "System.Object[]" },
        MinimumVersion = "3.0.0",
        MaximumVersion = "3.*.*",
        IntegrationName = IntegrationName)]
    public class AzureFunctionInvokerInvokeAsyncIntegration
    {
        private const string IntegrationName = nameof(IntegrationIds.AzureFunction);
        private static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="instanceParam">Instance for the function</param>
        /// <param name="argumentsParam">Arguments to pass to the function</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, object instanceParam, object[] argumentsParam)
            where TTarget : IFunctionInvoker
        {
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn<Task<object>> OnMethodEnd<TTarget>(TTarget instance, Task<object> returnValue, Exception exception, CallTargetState state)
        {
            // state.Scope.DisposeWithException(exception);
            return new CallTargetReturn<Task<object>>(returnValue);
        }
    }
}
