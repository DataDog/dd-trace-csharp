// <copyright file="AzureFunctionTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class AzureFunctionTags : InstrumentationTags
    {
        protected static readonly IProperty<string>[] AzureFunctionTagsProperties =
            InstrumentationTagsProperties.Concat(
                new ReadOnlyProperty<AzureFunctionTags, string>(Trace.Tags.InstrumentationName, t => t.InstrumentationName),
                new Property<AzureFunctionTags, string>(Trace.Tags.AzureFunctionTriggerType, t => t.TriggerType, (t, v) => t.TriggerType = v),
                new Property<AzureFunctionTags, string>(Trace.Tags.AzureFunctionShortName, t => t.ShortName, (t, v) => t.ShortName = v),
                new Property<AzureFunctionTags, string>(Trace.Tags.AzureFunctionFullName, t => t.FullName, (t, v) => t.FullName = v),
                new Property<AzureFunctionTags, string>(Trace.Tags.AzureFunctionClassName, t => t.ClassName, (t, v) => t.ClassName = v));

        public AzureFunctionTags()
        {
            SpanKind = SpanKinds.Server;
            TriggerType = "Unknown";
        }

        public override string SpanKind { get; }

        public string InstrumentationName { get; set; }

        public string ShortName { get; set; }

        public string FullName { get; set; }

        public string ClassName { get; set; }

        public string TriggerType { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => AzureFunctionTagsProperties;
    }
}
