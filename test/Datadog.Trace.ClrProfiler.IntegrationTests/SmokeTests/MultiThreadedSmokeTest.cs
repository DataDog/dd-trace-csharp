using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class MultiThreadedSmokeTest : SmokeTestBase
    {
        public MultiThreadedSmokeTest(ITestOutputHelper output)
            : base(output, "DataDogThreadTest")
        {
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            if (!EnvironmentHelper.IsCoreClr())
            {
                Output.WriteLine("Ignored for .NET Framework");
                return;
            }

            CheckForSmoke();
        }
    }
}
