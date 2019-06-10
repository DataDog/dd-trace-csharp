using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class AspNetCoreMvc2Tests : TestHelper
    {
        public AspNetCoreMvc2Tests(ITestOutputHelper output)
            : base("AspNetCoreMvc2", output)
        {
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [InlineData("/")]
        [InlineData("/api/delay/0")]
        public void SubmitsTracesSelfHosted(string path)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            int aspNetCorePort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (Process process = StartSample(agent.Port, arguments: null, packageVersion: string.Empty, aspNetCorePort: aspNetCorePort))
            {
                var wh = new EventWaitHandle(false, EventResetMode.AutoReset);

                process.OutputDataReceived += (sender, args) =>
                                              {
                                                  if (args.Data != null)
                                                  {
                                                      if (args.Data.Contains("Now listening on:") || args.Data.Contains("Unable to start Kestrel"))
                                                      {
                                                          wh.Set();
                                                      }

                                                      Output.WriteLine($"[webserver][stdout] {args.Data}");
                                                  }
                                              };
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (sender, args) =>
                                             {
                                                 if (args.Data != null)
                                                 {
                                                     Output.WriteLine($"[webserver][stderr] {args.Data}");
                                                 }
                                             };
                process.BeginErrorReadLine();

                // wait for server to start
                wh.WaitOne(5000);

                try
                {
                    var request = WebRequest.Create($"http://localhost:{aspNetCorePort}{path}");
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        Output.WriteLine($"[http] {response.StatusCode} {reader.ReadToEnd()}");
                    }
                }
                catch (WebException wex)
                {
                    Output.WriteLine($"[http] exception: {wex}");
                    if (wex.Response is HttpWebResponse response)
                    {
                        using (var stream = response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            Output.WriteLine($"[http] {response.StatusCode} {reader.ReadToEnd()}");
                        }
                    }
                }

                var spans = agent.WaitForSpans(1);
                if (!process.HasExited)
                {
                    process.Kill();
                }

                Assert.True(spans.Count > 0, "expected at least one span");
                foreach (var span in spans)
                {
                    Assert.Equal("aspnet-coremvc.request", span.Name);
                    Assert.Equal(SpanTypes.Web, span.Type);
                    Assert.Equal($"{path}", span.Resource);
                }
            }
        }
    }
}
