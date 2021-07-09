// <copyright file="MockTracerAgentExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Immutable;
using Datadog.Trace.Abstractions;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class MockTracerAgentExtensions
    {
        public IImmutableList<Span> WaitForAppSecEvents(
          int count,
          int timeoutInMilliseconds = 20000,
          string operationName = null,
          DateTimeOffset? minDateTime = null,
          bool returnAllOperations = false)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutInMilliseconds);
            var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

            IImmutableList<IEvent> relevantSpans = ImmutableList<IEvent>.Empty;
            return null;
        }
    }
}
