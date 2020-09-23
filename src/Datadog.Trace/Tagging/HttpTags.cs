using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class HttpTags : CommonTags
    {
        private const string HttpClientHandlerTypeKey = "http-client-handler-type";

        private static new readonly IProperty<string>[] TagsProperties =
            CommonTags.TagsProperties.Concat(
                new Property<HttpTags, string>(Trace.Tags.Env, t => t.Environment, (t, v) => t.Environment = v),
                new Property<HttpTags, string>(Trace.Tags.Version, t => t.Version, (t, v) => t.Version = v),
                new Property<HttpTags, string>(Trace.Tags.HttpStatusCode, t => t.HttpStatusCode, (t, v) => t.HttpStatusCode = v),
                new Property<HttpTags, string>(HttpClientHandlerTypeKey, t => t.HttpClientHandlerType, (t, v) => t.HttpClientHandlerType = v),
                new Property<HttpTags, string>(Trace.Tags.SpanKind, t => t.SpanKind, (t, v) => t.SpanKind = v),
                new Property<HttpTags, string>(Trace.Tags.HttpMethod, t => t.HttpMethod, (t, v) => t.HttpMethod = v),
                new Property<HttpTags, string>(Trace.Tags.HttpUrl, t => t.HttpUrl, (t, v) => t.HttpUrl = v),
                new Property<HttpTags, string>(Trace.Tags.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v));

        private static new readonly IProperty<double?>[] MetricsProperties =
            CommonTags.MetricsProperties.Concat(
                new Property<HttpTags, double?>(Trace.Tags.Analytics, t => t.AnalyticsSampleRate, (t, v) => t.AnalyticsSampleRate = v));

        public string SpanKind { get; set; }

        public string HttpMethod { get; set; }

        public string HttpUrl { get; set; }

        public string InstrumentationName { get; set; }

        public string HttpClientHandlerType { get; set; }

        public string HttpStatusCode { get; set; }

        public double? AnalyticsSampleRate { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => TagsProperties;

        protected override IProperty<double?>[] GetAdditionalMetrics() => MetricsProperties;
    }
}
