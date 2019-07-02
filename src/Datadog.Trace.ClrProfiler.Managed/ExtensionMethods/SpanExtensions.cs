using Datadog.Trace.Interfaces;

namespace Datadog.Trace.ClrProfiler.ExtensionMethods
{
    internal static class SpanExtensions
    {
        internal static string GetHttpMethod(this ISpan span)
            => span.GetTag(Tags.HttpMethod);

        internal static string GetHost(this ISpan span)
            => span.GetTag(Tags.HttpRequestHeadersHost);

        internal static string GetAbsoluteUrl(this ISpan span)
            => span.GetTag(Tags.HttpUrl);

        internal static void DecorateWebServerSpan(
            this Span span,
            string resourceName,
            string method,
            string host,
            string httpUrl)
        {
            span.Type = SpanTypes.Web;
            span.ResourceName = resourceName?.Trim();
            span.SetTag(Tags.SpanKind, SpanKinds.Server);
            span.SetTag(Tags.HttpMethod, method);
            span.SetTag(Tags.HttpRequestHeadersHost, host);
            span.SetTag(Tags.HttpUrl, httpUrl);
        }
    }
}
