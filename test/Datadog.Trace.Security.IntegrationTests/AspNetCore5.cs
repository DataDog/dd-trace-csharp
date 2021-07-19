// <copyright file="AspNetCore5.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NET5_0
using System;
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
            var (statusCode, _) = await SubmitRequest("/Home?arg=[$slice]");
            Assert.Equal(expectedStatusCode, statusCode);
            var spans = agent.WaitForSpans(2, returnAllOperations: true);
            Assert.Equal(2, spans.Count);
            foreach (var span in spans)
            {
                Assert.Equal("aspnet_core.request", span.Name);
                Assert.Equal("Samples.AspNetCore5", span.Service);
                Assert.Equal("web", span.Type);
            }

            var expectedAppSecEvents = enableSecurity ? 1 : 0;
            var appSecEvents = mockTracerAgentAppSecWrapper.WaitForAppSecEvents(expectedAppSecEvents);
            Assert.Equal(expectedAppSecEvents, appSecEvents.Count);
            mockTracerAgentAppSecWrapper.UnsubscribeAppSecEvents();
        }
    }
}
#endif
