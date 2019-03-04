using System.Net;
using System.Net.Http;
using Datadog.Trace.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class HttpHeadersPropagatorTests
    {
        [Fact]
        public void HttpRequestMessage_InjectExtract_Identity()
        {
            const int traceId = 9;
            const int spanId = 7;
            var headers = new HttpRequestMessage().Headers;
            var context = new SpanContext(traceId, spanId);

            headers.Inject(context);

            var resultContext = headers.Extract();

            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
        }

        [Fact]
        public void WebRequest_InjectExtract_Identity()
        {
            const int traceId = 9;
            const int spanId = 7;
            var headers = WebRequest.CreateHttp("http://localhost").Headers;
            var context = new SpanContext(traceId, spanId);

            headers.Inject(context);

            var resultContext = headers.Extract();

            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
        }
    }
}
