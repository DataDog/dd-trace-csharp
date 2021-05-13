﻿using System;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class KafkaTags : InstrumentationTags
    {
        private const string ComponentName = "kafka";

        private static readonly IProperty<string>[] KafkaTagsProperties =
            InstrumentationTagsProperties.Concat(
                new ReadOnlyProperty<KafkaTags, string>(Trace.Tags.InstrumentationName, t => t.InstrumentationName),
                new Property<KafkaTags, string>(Trace.Tags.KafkaPartition, t => t.Partition, (t, v) => t.Partition = v),
                new Property<KafkaTags, string>(Trace.Tags.KafkaOffset, t => t.Offset, (t, v) => t.Offset = v),
                new Property<KafkaTags, string>(Trace.Tags.KafkaTombstone, t => t.Tombstone, (t, v) => t.Tombstone = v),
                new Property<KafkaTags, string>(Trace.Tags.MessageQueueTimeMs, t => t.RecordQueueTimeMs, (t, v) => t.RecordQueueTimeMs = v));

        // For the sake of unit tests, define a default constructor
        // though the Kafka integration should use the constructor that takes a spanKind
        // so the setter is only invoked once
        [Obsolete("Use constructor that takes a SpanKind")]
        public KafkaTags()
            : this(SpanKinds.Producer)
        {
        }

        public KafkaTags(string spanKind)
        {
            SpanKind = spanKind;
        }

        public override string SpanKind { get; }

        public string InstrumentationName => ComponentName;

        public string Partition { get; set; }

        public string Offset { get; set; }

        public string Tombstone { get; set; }

        public string RecordQueueTimeMs { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => KafkaTagsProperties;
    }
}
