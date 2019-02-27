using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class HttpClientTests : TestHelper
    {
        public HttpClientTests(ITestOutputHelper output)
            : base("HttpMessageHandler", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void HttpClient()
        {
            const int agentPort = 9006;

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agentPort, "HttpClient"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1);
                Assert.True(spans.Count > 0, "expected at least one span");

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.ParentId);

                foreach (var span in spans)
                {
                    Assert.Equal("http.request", span.Name);
                    Assert.Equal("Samples.HttpMessageHandler-http-client", span.Service);
                    Assert.Equal(SpanTypes.Http, span.Type);
                    Assert.Equal(nameof(HttpMessageHandler), span.Tags[Tags.InstrumentationName]);

                    Assert.Equal(span.TraceId.ToString(CultureInfo.InvariantCulture), traceId);
                    Assert.Equal(span.SpanId.ToString(CultureInfo.InvariantCulture), parentSpanId);
                }
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void HttpClient_TracingDisabled()
        {
            const int agentPort = 9007;

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agentPort, "HttpClient TracingDisabled"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.Equal(0, spans.Count);

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.ParentId);
                var tracingEnabled = GetHeader(processResult.StandardOutput, HttpHeaderNames.TracingEnabled);

                Assert.Null(traceId);
                Assert.Null(parentSpanId);
                Assert.Equal("false", tracingEnabled);
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void WebClient()
        {
            const int agentPort = 9008;

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agentPort, "WebClient"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1);
                Assert.True(spans.Count > 0, "expected at least one span");

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.ParentId);

                // inspect the top-level span, underlying spans can be HttpMessageHandler in .NET Core
                foreach (var span in spans.Take(1))
                {
                    Assert.Equal("http.request", span.Name);
                    Assert.Equal("Samples.HttpMessageHandler-http-client", span.Service);
                    Assert.Equal(SpanTypes.Http, span.Type);
                    Assert.Equal(nameof(WebRequest), span.Tags[Tags.InstrumentationName]);

                    Assert.Equal(span.TraceId.ToString(CultureInfo.InvariantCulture), traceId);
                    Assert.Equal(span.SpanId.ToString(CultureInfo.InvariantCulture), parentSpanId);
                }
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void WebClient_TracingDisabled()
        {
            const int agentPort = 9009;

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agentPort, "WebClient TracingDisabled"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.Equal(0, spans.Count);

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.ParentId);
                var tracingEnabled = GetHeader(processResult.StandardOutput, HttpHeaderNames.TracingEnabled);

                Assert.Null(traceId);
                Assert.Null(parentSpanId);
                Assert.Equal("false", tracingEnabled);
            }
        }

        private string GetHeader(string stdout, string name)
        {
            var pattern = $@"^\[HttpListener\] request header: {name}=(\w+)\r?$";
            var match = Regex.Match(stdout, pattern, RegexOptions.Multiline);

            return match.Success
                       ? match.Groups[1].Value
                       : null;
        }
    }
}
