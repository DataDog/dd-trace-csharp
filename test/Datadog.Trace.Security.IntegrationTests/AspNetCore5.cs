// <copyright file="AspNetCore5.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NET5_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class AspNetCore5 : AspNetCoreBase, IDisposable
    {
        public AspNetCore5(ITestOutputHelper outputHelper)
            : base("AspNetCore5", outputHelper)
        {
        }

        [Theory]
        [InlineData(true, HttpStatusCode.Forbidden)]
        [InlineData(false, HttpStatusCode.OK)]
        public async Task TestBlockedRequestAsync(bool enableSecurity, HttpStatusCode expectedStatusCode)
        {
            using var agent = await RunOnSelfHosted(enableSecurity);
            var mockTracerAgentAppSecWrapper = new MockTracerAgentAppSecWrapper(agent);
            mockTracerAgentAppSecWrapper.SubscribeAppSecEvents();
            Func<Task<(HttpStatusCode StatusCode, string ResponseText)>> attack = () => SubmitRequest("?arg=[$slice]");
            var resultRequests = await Task.WhenAll(attack(), attack(), attack(), attack(), attack());
            var expectedSpans = 6; // one more because runselfhosted pings once
            var spans = agent.WaitForSpans(expectedSpans);
            Assert.Equal(expectedSpans, spans.Count);
            foreach (var span in spans)
            {
                Assert.Equal("aspnet_core.request", span.Name);
                Assert.Equal("Samples.AspNetCore5", span.Service);
                Assert.Equal("web", span.Type);
            }

            var expectedAppSecEvents = enableSecurity ? 5 : 0;
            var appSecEvents = mockTracerAgentAppSecWrapper.WaitForAppSecEvents(expectedAppSecEvents);
            Assert.Equal(expectedAppSecEvents, appSecEvents.Count);
            Assert.All(resultRequests, r => Assert.Equal(r.StatusCode, expectedStatusCode));
            var spanIds = spans.Select(s => s.SpanId);
            var usedIds = new List<ulong>();
            foreach (var item in appSecEvents)
            {
                Assert.IsType<AppSec.EventModel.Attack>(item);
                var attackEvent = (AppSec.EventModel.Attack)item;
                Assert.True(attackEvent.Blocked);
                var spanId = spanIds.FirstOrDefault(s => s == attackEvent.Context.Span.Id);
                Assert.NotEqual(0m, spanId);
                Assert.DoesNotContain(spanId, usedIds);
                Assert.Equal("nosql_injection-monitoring", attackEvent.Rule.Name);
                usedIds.Add(spanId);
            }

            mockTracerAgentAppSecWrapper.UnsubscribeAppSecEvents();
        }
    }
}
#endif
