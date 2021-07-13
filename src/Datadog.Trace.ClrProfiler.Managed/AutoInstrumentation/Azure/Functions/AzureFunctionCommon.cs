// <copyright file="AzureFunctionCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
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

        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, IFunctionInstance instanceParam)
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

        internal static Scope CreateScope(Tracer tracer, IFunctionInstance instanceParam)
        {
            Scope scope = null;

            try
            {
                var bindingSourceObject = instanceParam.BindingSource;

                var functionName = instanceParam.FunctionDescriptor.ShortName;
                var fullName = instanceParam.FunctionDescriptor.FullName;
                var parts = fullName.Split('.');
                var className = parts[parts.Length - 2];

                var tags = new AzureFunctionTags();
                var serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);
                var resourceName = $"{className}.{functionName}";
                SpanContext propagatedContext = null;

                switch (instanceParam.Reason)
                {
                    case AzureFunctionExecutionReason.HostCall:
                        DecorateHttpTrigger(tracer, bindingSourceObject, tags, ref propagatedContext, ref resourceName);
                        break;
                    case AzureFunctionExecutionReason.AutomaticTrigger:
                        tags.TriggerType = "Timer";
                        break;
                    case AzureFunctionExecutionReason.Dashboard:
                        tags.TriggerType = "Dashboard";
                        break;
                }

                scope = tracer.StartActiveWithTags(OperationName, parent: propagatedContext, tags: tags);
                var span = scope.Span;

                span.ResourceName = resourceName;
                span.Type = SpanTypes.AzureFunction;
                tags.ShortName = functionName;
                tags.FullName = fullName;
                tags.ClassName = className;
                tags.InstrumentationName = IntegrationId.Name;
                tags.SetAnalyticsSampleRate(IntegrationId, tracer.Settings, enabledWithGlobalSetting: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            // always returns the scope, even if it's null because we couldn't create it,
            // or we couldn't populate it completely (some tags is better than no tags)
            return scope;
        }

        private static void DecorateHttpTrigger(
            Tracer tracer,
            object bindingSourceObject,
            AzureFunctionTags tags,
            ref SpanContext propagatedContext,
            ref string resourceName)
        {
            try
            {
                var castedBindingSource = bindingSourceObject.DuckCast<IHttpTriggerBindingSource>();

                foreach (var parameter in castedBindingSource.Parameters)
                {
                    var value = parameter.Value;
                    var valueType = value.GetType();
                    if (valueType.FullName?.Equals("Microsoft.AspNetCore.Http.DefaultHttpRequest") ?? false)
                    {
                        tags.TriggerType = "Http";
                        var castedRequest = value.DuckCast<IDefaultHttpRequest>();
                        resourceName = $"GET {castedRequest.Path}";

                        // extract propagated http headers
                        var wrappedHeaders = new HttpHeadersCollection(castedRequest.Headers);
                        propagatedContext = SpanContextPropagator.Instance.Extract(wrappedHeaders);
                        var tagsFromHeaders = SpanContextPropagator.Instance.ExtractHeaderTags(wrappedHeaders, tracer.Settings.HeaderTags, defaultTagPrefix: SpanContextPropagator.HttpRequestHeadersTagPrefix);
                        foreach (var kvp in tagsFromHeaders)
                        {
                            tags.SetTag(kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error collecting metadata from HttpRequest.");
            }
        }

        private static IEnumerable<string> GetHeaders(IRequestHeaders headers, string headerName)
        {
            if (headers.TryGetValues(headerName, out var headerValues))
            {
                foreach (var headerValue in headerValues)
                {
                    yield return headerValue;
                }
            }
        }
    }
}
