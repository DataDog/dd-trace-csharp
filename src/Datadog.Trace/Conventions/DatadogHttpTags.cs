using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Conventions
{
    internal class DatadogHttpTags : HttpTags
    {
        protected static readonly IProperty<string>[] HttpTagsProperties =
            InstrumentationTagsProperties.Concat(
                new Property<HttpTags, string>(Trace.Tags.HttpStatusCode, t => t.HttpStatusCode, (t, v) => t.HttpStatusCode = v),
                new Property<HttpTags, string>(HttpClientHandlerTypeKey, t => t.HttpClientHandlerType, (t, v) => t.HttpClientHandlerType = v),
                new Property<HttpTags, string>(Trace.Tags.HttpMethod, t => t.HttpMethod, (t, v) => t.HttpMethod = v),
                new Property<HttpTags, string>(Trace.Tags.HttpUrl, t => t.HttpUrl, (t, v) => t.HttpUrl = v),
                new Property<HttpTags, string>(Trace.Tags.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v));

        private const string HttpClientHandlerTypeKey = "http-client-handler-type";

        protected override IProperty<string>[] GetAdditionalTags() => HttpTagsProperties;
    }
}