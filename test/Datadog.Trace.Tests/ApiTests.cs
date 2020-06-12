using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using Datadog.Trace.TestHelpers.HttpMessageHandlers;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ApiTests
    {
        private readonly Tracer _tracer;

        public ApiTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _tracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Fact(Skip = "Skip for now while I figure out to more easily mock this")]
        public async Task SendTraceAsync_200OK_AllGood()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            };
            var handler = new SetResponseHandler(response);
            var api = new Api(new Uri("http://localhost:1234"), statsd: null);

            var span = _tracer.StartSpan("Operation");
            var traces = new[] { new[] { span } };
            await api.SendTracesAsync(traces);

            Assert.Equal(1, handler.RequestsCount);
        }

        [Fact(Skip = "Skip for now while I figure out to more easily mock this")]
        public async Task SendTracesAsync_500_ErrorIsCaught()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };
            var handler = new SetResponseHandler(response);
            var api = new Api(new Uri("http://localhost:1234"), statsd: null);

            var sw = new Stopwatch();
            sw.Start();
            var span = _tracer.StartSpan("Operation");
            var traces = new[] { new[] { span } };
            await api.SendTracesAsync(traces);
            sw.Stop();

            Assert.Equal(5, handler.RequestsCount);
            Assert.InRange(sw.ElapsedMilliseconds, 1000, 16000); // should be ~ 3200ms

            // TODO:bertrand check that it's properly logged
        }
    }
}
