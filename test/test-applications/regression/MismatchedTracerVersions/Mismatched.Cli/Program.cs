using System;
using System.Net.Http;
using System.Threading.Tasks;
using Datadog.Trace;

namespace MismatchedTracerVersions.Cli
{
    public static class Program
    {
        public static async Task Main()
        {
            using var scope = Tracer.Instance.StartActive("main");

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    HttpClient httpClient = new();

                    // we expect this to fail, but we're only insterested in instrumenting the call
                    string timestamp = await httpClient.GetStringAsync("http://localhost/timestamp");
                }
                catch (Exception)
                {
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // await Tracer.Instance.ForceFlushAsync();
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
