namespace Datadog.Trace.Ci
{
    /// <summary>
    /// Span tags for test data model
    /// </summary>
    internal static class TestTags
    {
        /// <summary>
        /// Test suite name
        /// </summary>
        public const string Suite = "test.suite";

        /// <summary>
        /// Test name
        /// </summary>
        public const string Name = "test.name";

        /// <summary>
        /// Test fqn
        /// </summary>
        public const string Fqn = "test.fqn";

        /// <summary>
        /// Test fqn
        /// </summary>
        public const string Id = "test.id";

        /// <summary>
        /// Test type
        /// </summary>
        public const string Type = "test.type";

        /// <summary>
        /// Test type test
        /// </summary>
        public const string TypeTest = "test";

        /// <summary>
        /// Test type benchmark
        /// </summary>
        public const string TypeBenchmark = "benchmark";

        /// <summary>
        /// Test fqn
        /// </summary>
        public const string ProcessId = "test.processId";

        /// <summary>
        /// Test framework
        /// </summary>
        public const string Framework = "test.framework";

        /// <summary>
        /// Test parameters
        /// </summary>
        public const string Arguments = "test.arguments";

        /// <summary>
        /// Test traits
        /// </summary>
        public const string Traits = "test.traits";

        /// <summary>
        /// Test status
        /// </summary>
        public const string Status = "test.status";

        /// <summary>
        /// Test Pass status
        /// </summary>
        public const string StatusPass = "pass";

        /// <summary>
        /// Test Fail status
        /// </summary>
        public const string StatusFail = "fail";

        /// <summary>
        /// Test Skip status
        /// </summary>
        public const string StatusSkip = "skip";

        /// <summary>
        /// Test skip reason
        /// </summary>
        public const string SkipReason = "test.skip_reason";

        /// <summary>
        /// GIT Repository
        /// </summary>
        public const string GitRepository = "git.repository";

        /// <summary>
        /// GIT Commit hash
        /// </summary>
        public const string GitCommit = "git.commit";

        /// <summary>
        /// GIT Branch name
        /// </summary>
        public const string GitBranch = "git.branch";

        /// <summary>
        /// Build Source root
        /// </summary>
        public const string BuildSourceRoot = "build.source_root";

        /// <summary>
        /// Build InContainer flag
        /// </summary>
        public const string BuildInContainer = "build.incontainer";

        /// <summary>
        /// CI Provider
        /// </summary>
        public const string CIProvider = "ci.provider";

        /// <summary>
        /// CI Pipeline id
        /// </summary>
        public const string CIPipelineId = "pipeline.id";

        /// <summary>
        /// CI Pipeline number
        /// </summary>
        public const string CIPipelineNumber = "pipeline.number";

        /// <summary>
        /// CI Pipeline url
        /// </summary>
        public const string CIPipelineUrl = "pipeline.url";

        /// <summary>
        /// Runtime name
        /// </summary>
        public const string RuntimeName = "runtime.name";

        /// <summary>
        /// Runtime os architecture
        /// </summary>
        public const string RuntimeOSArchitecture = "runtime.os_architecture";

        /// <summary>
        /// Runtime os platform
        /// </summary>
        public const string RuntimeOSPlatform = "runtime.os_platform";

        /// <summary>
        /// Runtime process architecture
        /// </summary>
        public const string RuntimeProcessArchitecture = "runtime.process_architecture";

        /// <summary>
        /// Runtime version
        /// </summary>
        public const string RuntimeVersion = "runtime.version";
    }
}
