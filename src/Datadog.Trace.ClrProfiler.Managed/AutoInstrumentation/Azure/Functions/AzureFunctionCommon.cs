// <copyright file="AzureFunctionCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    internal static class AzureFunctionCommon
    {
        private const string OperationName = "azure.function";
        private const string ServiceName = "azure-func";
        private static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(nameof(IntegrationIds.AzureFunction));

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AzureFunctionCommon));

        public static CallTargetState OnMethodBegin<TTarget, TFunction>(TTarget instance, TFunction instanceParam)
            where TFunction : IFunctionInstance
        {
            var tracer = Tracer.Instance;

            if (tracer.Settings.IsIntegrationEnabled(IntegrationId))
            {
                var scope = CreateScope(tracer, instanceParam);
                return new CallTargetState(scope);
            }

            return CallTargetState.GetDefault();
        }

        public static TResult OnMethodEnd<TTarget, TResult>(TTarget instance, TResult result, Exception exception, CallTargetState state)
        {
            Scope scope = state.Scope;

            if (scope is null)
            {
                return result;
            }

            try
            {
                if (exception != null)
                {
                    scope.Span.SetException(exception);
                }
            }
            finally
            {
                scope.Dispose();
            }

            return result;
        }

        internal static Scope CreateScope<TFunction>(Tracer tracer, TFunction instanceParam)
            where TFunction : IFunctionInstance
        {
            Scope scope = null;

            try
            {
                var functionName = instanceParam.FunctionDescriptor.ShortName;
                var fullName = instanceParam.FunctionDescriptor.FullName;
                var parts = fullName.Split('.');
                var className = parts[parts.Length - 2];

                var tags = new AzureFunctionTags();
                string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);

                var bindingSource = instanceParam.BindingSource;

                var fi = bindingSource.GetType().GetField("_parameters", BindingFlags.NonPublic | BindingFlags.Instance);
                var parametersUncasted = fi.GetValue(bindingSource);
                var parameters = (IDictionary<string, object>)parametersUncasted;

                foreach (var parameter in parameters)
                {
                    var value = parameter.Value;
                    var valueType = value.GetType();
                    if (valueType.FullName.Equals("Microsoft.AspNetCore.Http.DefaultHttpRequest"))
                    {
                        tags.TriggerType = "Http";
                    }
                    else if (valueType.FullName.Equals("Microsoft.Azure.WebJobs.TimerInfo"))
                    {
                        tags.TriggerType = "Timer";
                    }
                }

                scope = tracer.StartActiveWithTags(OperationName, tags: tags, serviceName: serviceName);
                var span = scope.Span;

                span.Type = SpanTypes.AzureFunction;
                span.ResourceName = $"{className}.{functionName}";
                tags.ShortName = functionName;
                tags.FullName = fullName;
                tags.ClassName = className;
                tags.InstrumentationName = IntegrationId.Name;
                tags.SetAnalyticsSampleRate(IntegrationId, tracer.Settings, enabledWithGlobalSetting: false);

                // instanceParam.BindingSource.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            // always returns the scope, even if it's null because we couldn't create it,
            // or we couldn't populate it completely (some tags is better than no tags)
            return scope;
        }
    }
}
