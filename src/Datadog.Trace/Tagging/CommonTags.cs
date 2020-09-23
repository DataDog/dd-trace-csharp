namespace Datadog.Trace.Tagging
{
    internal class CommonTags : TagsList
    {
        internal static readonly IProperty<double?>[] MetricsProperties =
        {
            new Property<CommonTags, double?>(Trace.Metrics.SamplingLimitDecision, t => t.SamplingLimitDecision, (t, v) => t.SamplingLimitDecision = v),
            new Property<CommonTags, double?>(Trace.Metrics.SamplingPriority, t => t.SamplingPriority, (t, v) => t.SamplingPriority = v)
        };

        internal static readonly IProperty<string>[] TagsProperties =
        {
            new Property<CommonTags, string>(Trace.Tags.Env, t => t.Environment, (t, v) => t.Environment = v),
            new Property<CommonTags, string>(Trace.Tags.Version, t => t.Version, (t, v) => t.Version = v)
        };

        public string Environment { get; set; }

        public string Version { get; set; }

        public double? SamplingPriority { get; set; }

        public double? SamplingLimitDecision { get; set; }

        protected override IProperty<double?>[] GetAdditionalMetrics() => MetricsProperties;

        protected override IProperty<string>[] GetAdditionalTags() => TagsProperties;
    }
}
