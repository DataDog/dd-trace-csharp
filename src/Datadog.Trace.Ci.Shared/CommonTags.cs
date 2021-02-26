namespace Datadog.Trace.Ci
{
    /// <summary>
    /// Common Span tags for test/build data model
    /// </summary>
    internal static class CommonTags
    {
        /// <summary>
        /// GIT Repository
        /// </summary>
        public const string GitRepository = "git.repository_url";

        /// <summary>
        /// GIT Commit hash
        /// </summary>
        public const string GitCommit = "git.commit.sha";

        /// <summary>
        /// GIT Branch name
        /// </summary>
        public const string GitBranch = "git.branch";

        /// <summary>
        /// GIT Tag name
        /// </summary>
        public const string GitTag = "git.tag";

        /// <summary>
        /// GIT Commit Author name
        /// </summary>
        public const string GitCommitAuthorName = "git.commit.author.name";

        /// <summary>
        /// GIT Commit Author email
        /// </summary>
        public const string GitCommitAuthorEmail = "git.commit.author.email";

        /// <summary>
        /// GIT Commit Author date
        /// </summary>
        public const string GitCommitAuthorDate = "git.commit.author.date";

        /// <summary>
        /// GIT Commit Committer name
        /// </summary>
        public const string GitCommitCommitterName = "git.commit.committer.name";

        /// <summary>
        /// GIT Commit Committer email
        /// </summary>
        public const string GitCommitCommitterEmail = "git.commit.committer.email";

        /// <summary>
        /// GIT Commit Committer date
        /// </summary>
        public const string GitCommitCommitterDate = "git.commit.committer.date";

        /// <summary>
        /// GIT Commit message
        /// </summary>
        public const string GitCommitMessage = "git.commit.message";

        /// <summary>
        /// Build Source root
        /// </summary>
        public const string BuildSourceRoot = "build.source_root";

        /// <summary>
        /// CI Provider
        /// </summary>
        public const string CIProvider = "ci.provider.name";

        /// <summary>
        /// CI Pipeline id
        /// </summary>
        public const string CIPipelineId = "ci.pipeline.id";

        /// <summary>
        /// CI Pipeline name
        /// </summary>
        public const string CIPipelineName = "ci.pipeline.name";

        /// <summary>
        /// CI Pipeline number
        /// </summary>
        public const string CIPipelineNumber = "ci.pipeline.number";

        /// <summary>
        /// CI Pipeline url
        /// </summary>
        public const string CIPipelineUrl = "ci.pipeline.url";

        /// <summary>
        /// CI Job url
        /// </summary>
        public const string CIJobUrl = "ci.job.url";

        /// <summary>
        /// CI Job Name
        /// </summary>
        public const string CIJobName = "ci.job.name";

        /// <summary>
        /// CI Stage Name
        /// </summary>
        public const string StageName = "ci.stage.name";

        /// <summary>
        /// CI Job url
        /// </summary>
        public const string CIWorkspacePath = "ci.workspace_path";

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
