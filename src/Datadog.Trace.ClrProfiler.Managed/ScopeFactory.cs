using System;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Convenience class that creates scopes and populates them with some standard details.
    /// </summary>
    internal static class ScopeFactory
    {
        public const string OperationName = "http.request";
        public const string ServiceName = "http-client";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(ScopeFactory));

        /// <summary>
        /// Creates a scope for outbound http requests and populates some common details.
        /// </summary>
        /// <param name="tracer">The tracer instance to use to create the new scope.</param>
        /// <param name="httpMethod">The HTTP method used by the request.</param>
        /// <param name="requestUri">The URI requested by the request.</param>
        /// <param name="integrationName">The name of the integration creating this scope.</param>
        /// <returns>A new pre-populated scope.</returns>
        public static Scope CreateOutboundHttpScope(Tracer tracer, string httpMethod, Uri requestUri, string integrationName)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                scope = tracer.StartActive(OperationName);
                var span = scope.Span;

                span.Type = SpanTypes.Http;
                span.ServiceName = $"{tracer.DefaultServiceName}-{ServiceName}";

                span.ResourceName = string.Join(
                    " ",
                    httpMethod,
                    UriHelpers.CleanUri(requestUri, removeScheme: true, tryRemoveIds: true));

                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                span.SetTag(Tags.HttpMethod, httpMethod?.ToUpperInvariant());
                span.SetTag(Tags.HttpUrl, UriHelpers.CleanUri(requestUri, removeScheme: false, tryRemoveIds: false));
                span.SetTag(Tags.InstrumentationName, integrationName);

                // set analytics sample rate if enabled
                var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationName, enabledWithGlobalSetting: false);
                span.SetMetric(Tags.Analytics, analyticsSampleRate);
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
