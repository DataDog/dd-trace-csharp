using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Tracer integration for HttpMessageHandler.
    /// </summary>
    public static class HttpMessageHandlerIntegration
    {
        internal const string OperationName = "http.request";
        internal const string ServiceName = "http-client";

        // internal readonly  string Name = nameof(HttpMessageHandlerIntegration).Substring(0, )

        /// <summary>
        /// Instrumentation wrapper for <see cref="HttpMessageHandler.SendAsync"/>.
        /// </summary>
        /// <param name="handler">The <see cref="HttpMessageHandler"/> instance to instrument.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that represents the current HTTP request.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> that can be used to cancel this <c>async</c> operation.</param>
        /// <returns>Returns the value returned by the inner method call.</returns>
        [InterceptMethod(
            TargetAssembly = "System.Net.Http",
            TargetType = "System.Net.Http.HttpMessageHandler")]
        [InterceptMethod(
            TargetAssembly = "System.Net.Http",
            TargetType = "System.Net.Http.HttpClientHandler")]
        public static object SendAsync(
            object handler,
            object request,
            object cancellationTokenSource)
        {
            // HttpMessageHandler
            // Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            return SendAsyncInternal(
                (HttpMessageHandler)handler,
                (HttpRequestMessage)request,
                cancellationToken);
        }

        private static async Task<HttpResponseMessage> SendAsyncInternal(
            HttpMessageHandler handler,
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var executeAsync = DynamicMethodBuilder<Func<HttpMessageHandler, HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>
               .GetOrCreateMethodCallDelegate(
                    handler.GetType(),
                    nameof(SendAsync));

            if (!IsTracingEnabled(request))
            {
                return await executeAsync(handler, request, cancellationToken).ConfigureAwait(false);
            }

            using (var scope = CreateScope(handler, request))
            {
                try
                {
                    // add distributed tracing headers
                    request.Headers.Inject(scope.Span.Context);

                    HttpResponseMessage response = await executeAsync(handler, request, cancellationToken).ConfigureAwait(false);
                    scope.Span.SetTag(Tags.HttpStatusCode, ((int)response.StatusCode).ToString());
                    return response;
                }
                catch (Exception ex)
                {
                    scope.Span.SetException(ex);
                    throw;
                }
            }
        }

        private static bool IsTracingEnabled(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(HttpHeaderNames.TracingEnabled, out var headerValues))
            {
                if (headerValues.Any(s => string.Equals(s, "false", StringComparison.InvariantCultureIgnoreCase)))
                {
                    // tracing is disabled for this request via http header
                    return false;
                }
            }

            return true;
        }

        private static Scope CreateScope(HttpMessageHandler handler, HttpRequestMessage request)
        {
            var tracer = Tracer.Instance;

            string httpMethod = request.Method.ToString().ToUpperInvariant();
            string url = request.RequestUri.OriginalString;
            string resourceName = $"{httpMethod}";
            string serviceName = $"{tracer.DefaultServiceName}-{ServiceName}";

            var scope = tracer.StartActive(OperationName, serviceName: serviceName);
            var span = scope.Span;
            span.Type = SpanTypes.Http;
            span.ResourceName = resourceName;
            span.SetTag(Tags.HttpMethod, httpMethod);
            span.SetTag(Tags.HttpUrl, url);
            span.SetTag(Tags.InstrumentationName, nameof(HttpMessageHandlerIntegration).TrimEnd("Integration"));
            span.SetTag(Tags.InstrumentationMethod, $"{handler.GetType().FullName}.{nameof(SendAsync)}");

            return scope;
        }
    }
}
