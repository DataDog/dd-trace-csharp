using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class SqlCommandIntegrationTests : TestHelper
    {
        public SqlCommandIntegrationTests(ITestOutputHelper output)
            : base("SqlCommand", output)
        {
        }

        [Theory]
        [MemberData(nameof(PackageVersions.SqlServer), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitsTraces(string packageVersion)
        {
            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(4);
                Assert.True(spans.Count > 0, "expected at least one span");

                foreach (var span in spans)
                {
                    Assert.Equal("sql-server.query", span.Name);
                    Assert.Equal($"Samples.SqlCommand-sql-server", span.Service);
                    Assert.Equal(SpanTypes.Sql, span.Type);
                }
            }
        }
    }
}
