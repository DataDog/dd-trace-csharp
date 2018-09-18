using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class StackExchangeRedisTests : TestHelper
    {
        private const int AgentPort = 9003;

        public StackExchangeRedisTests(ITestOutputHelper output)
            : base("RedisCore", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            using (var agent = new MockTracerAgent(AgentPort))
            using (var processResult = RunSampleAndWaitForExit(AgentPort, arguments: "StackExchange"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.GetSpans().Where(s => s.Type == "redis").ToList();
                Assert.Equal(8, spans.Count);

                foreach (var span in spans)
                {
                    Assert.Equal("redis.command", span.Name);
                    Assert.Equal("redis", span.Service);
                    Assert.Equal("redis", span.Type);
                    Assert.Equal("localhost", span.Tags.Get<string>("out.host"));
                    Assert.Equal("6379", span.Tags.Get<string>("out.port"));
                }

                var expected = new TupleList<string, string>
                {
                    { "SET", "SET StackExchange.Redis.INCR" },
                    { "PING", "PING" },
                    { "DDCUSTOM", "DDCUSTOM" },
                    { "ECHO", "ECHO" },
                    { "SLOWLOG", "SLOWLOG" },
                    { "INCR", "INCR StackExchange.Redis.INCR" },
                    { "INCRBYFLOAT", "INCRBYFLOAT StackExchange.Redis.INCR" },
                    { "TIME", "TIME" },
                };

                for (int i = 0; i < expected.Count; i++)
                {
                    var e1 = expected[i].Item1;
                    var a1 = spans[i].Resource;

                    var e2 = expected[i].Item2;
                    var a2 = spans[i].Tags.Get<string>("redis.raw_command");

                    Assert.True(e1 == a1, $"invalid resource name for span {i}, {e1} != {a1}");
                    Assert.True(e2 == a2, $"invalid raw command for span {i}, {e2} != {a2}");
                }
            }
        }
    }
}
