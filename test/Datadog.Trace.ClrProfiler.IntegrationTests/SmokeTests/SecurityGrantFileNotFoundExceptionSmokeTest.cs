#if NET452 || NET461

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class SecurityGrantFileNotFoundExceptionSmokeTest : SmokeTestBase
    {
        public SecurityGrantFileNotFoundExceptionSmokeTest(ITestOutputHelper output)
            : base(output, "OrleansCrash", maxTestRunSeconds: 30)
        {
            AssumeSuccessOnTimeout = true;
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            CheckForSmoke();
        }
    }
}
#endif
