using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Datadog.Trace.Ci;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.CI
{
    public class GitParserTests
    {
        public static IEnumerable<object[]> GetData()
        {
            string dataFolder = Path.Combine(Environment.CurrentDirectory, "CI", "Data");

            // gitdata-01 => Git clone
            yield return new object[]
            {
                new TestItem(Path.Combine(dataFolder, "gitdata-01"))
                {
                    AuthorDate = "2/26/2021 6:32:13 PM +00:00",
                    AuthorEmail = "tony.redondo@datadoghq.com",
                    AuthorName = "Tony Redondo",
                    Branch = "refs/heads/master",
                    Commit = "5b6f3a6dab5972d73a56dff737bd08d995255c08",
                    CommitterDate = "2/26/2021 6:32:13 PM +00:00",
                    CommitterEmail = "noreply@github.com",
                    CommitterName = "GitHub",
                    Repository = "git@github.com:DataDog/dd-trace-dotnet.git",
                    SourceRoot = dataFolder
                },
            };

            // gitdata-02 => Git clone  + git gc (force packs files)
            yield return new object[]
            {
                new TestItem(Path.Combine(dataFolder, "gitdata-02"))
                {
                    AuthorDate = "2/26/2021 6:32:13 PM +00:00",
                    AuthorEmail = "tony.redondo@datadoghq.com",
                    AuthorName = "Tony Redondo",
                    Branch = "refs/heads/master",
                    Commit = "5b6f3a6dab5972d73a56dff737bd08d995255c08",
                    CommitterDate = "2/26/2021 6:32:13 PM +00:00",
                    CommitterEmail = "noreply@github.com",
                    CommitterName = "GitHub",
                    Repository = "git@github.com:DataDog/dd-trace-dotnet.git",
                    SourceRoot = dataFolder
                },
            };

            // gitdata-03 => Git clone + git checkout [sha]
            yield return new object[]
            {
                new TestItem(Path.Combine(dataFolder, "gitdata-03"))
                {
                    AuthorDate = "2/26/2021 6:32:13 PM +00:00",
                    AuthorEmail = "tony.redondo@datadoghq.com",
                    AuthorName = "Tony Redondo",
                    Branch = null,
                    Commit = "5b6f3a6dab5972d73a56dff737bd08d995255c08",
                    CommitterDate = "2/26/2021 6:32:13 PM +00:00",
                    CommitterEmail = "noreply@github.com",
                    CommitterName = "GitHub",
                    Repository = "git@github.com:DataDog/dd-trace-dotnet.git",
                    SourceRoot = dataFolder
                },
            };

            // gitdata-04 => Git clone + git checkout [sha] + git gc (force packs files)
            yield return new object[]
            {
                new TestItem(Path.Combine(dataFolder, "gitdata-04"))
                {
                    AuthorDate = "2/26/2021 6:32:13 PM +00:00",
                    AuthorEmail = "tony.redondo@datadoghq.com",
                    AuthorName = "Tony Redondo",
                    Branch = null,
                    Commit = "5b6f3a6dab5972d73a56dff737bd08d995255c08",
                    CommitterDate = "2/26/2021 6:32:13 PM +00:00",
                    CommitterEmail = "noreply@github.com",
                    CommitterName = "GitHub",
                    Repository = "git@github.com:DataDog/dd-trace-dotnet.git",
                    SourceRoot = dataFolder
                },
            };

            // gitdata-05 => Git clone + git gc + git checkout tag
            yield return new object[]
            {
                new TestItem(Path.Combine(dataFolder, "gitdata-05"))
                {
                    AuthorDate = "2/19/2021 12:59:01 PM +00:00",
                    AuthorEmail = "andrew.lock@datadoghq.com",
                    AuthorName = "Andrew Lock",
                    Branch = null,
                    Commit = "b667f427df9f9b0521b1b25ee0967896aa510012",
                    CommitterDate = "2/19/2021 12:59:01 PM +00:00",
                    CommitterEmail = "noreply@github.com",
                    CommitterName = "GitHub",
                    Repository = "git@github.com:DataDog/dd-trace-dotnet.git",
                    SourceRoot = dataFolder
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetData))]
        public void ExtractGitDataFromFolder(TestItem testItem)
        {
            Assert.True(Directory.Exists(testItem.GitFolderPath));

            var gitInfo = GitInfo.GetFrom(testItem.GitFolderPath);

            Assert.Equal(testItem.AuthorDate, gitInfo.AuthorDate.Value.ToString(new CultureInfo("en-us")));
            Assert.Equal(testItem.AuthorEmail, gitInfo.AuthorEmail);
            Assert.Equal(testItem.AuthorName, gitInfo.AuthorName);
            Assert.Equal(testItem.Branch, gitInfo.Branch);
            Assert.Equal(testItem.Commit, gitInfo.Commit);
            Assert.Equal(testItem.CommitterDate, gitInfo.CommitterDate.Value.ToString(new CultureInfo("en-us")));
            Assert.Equal(testItem.CommitterEmail, gitInfo.CommitterEmail);
            Assert.Equal(testItem.CommitterName, gitInfo.CommitterName);
            Assert.NotNull(gitInfo.Message);
            Assert.NotNull(gitInfo.PgpSignature);
            Assert.Equal(testItem.Repository, gitInfo.Repository);
            Assert.Equal(testItem.SourceRoot, gitInfo.SourceRoot);
        }

        public class TestItem : IXunitSerializable
        {
            public TestItem()
            {
            }

            internal TestItem(string gitFolderPath)
            {
                GitFolderPath = gitFolderPath;
            }

            internal string GitFolderPath { get; set; }

            internal string AuthorDate { get; set; }

            internal string AuthorEmail { get; set; }

            internal string AuthorName { get; set; }

            internal string Branch { get; set; }

            internal string Commit { get; set; }

            internal string CommitterDate { get; set; }

            internal string CommitterEmail { get; set; }

            internal string CommitterName { get; set; }

            internal string Repository { get; set; }

            internal string SourceRoot { get; set; }

            public void Deserialize(IXunitSerializationInfo info)
            {
                GitFolderPath = info.GetValue<string>(nameof(GitFolderPath));
                AuthorDate = info.GetValue<string>(nameof(AuthorDate));
                AuthorEmail = info.GetValue<string>(nameof(AuthorEmail));
                AuthorName = info.GetValue<string>(nameof(AuthorName));
                Branch = info.GetValue<string>(nameof(Branch));
                Commit = info.GetValue<string>(nameof(Commit));
                CommitterDate = info.GetValue<string>(nameof(CommitterDate));
                CommitterEmail = info.GetValue<string>(nameof(CommitterEmail));
                CommitterName = info.GetValue<string>(nameof(CommitterName));
                Repository = info.GetValue<string>(nameof(Repository));
                SourceRoot = info.GetValue<string>(nameof(SourceRoot));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(GitFolderPath), GitFolderPath);
                info.AddValue(nameof(AuthorDate), AuthorDate);
                info.AddValue(nameof(AuthorEmail), AuthorEmail);
                info.AddValue(nameof(AuthorName), AuthorName);
                info.AddValue(nameof(Branch), Branch);
                info.AddValue(nameof(Commit), Commit);
                info.AddValue(nameof(CommitterDate), CommitterDate);
                info.AddValue(nameof(CommitterEmail), CommitterEmail);
                info.AddValue(nameof(CommitterName), CommitterName);
                info.AddValue(nameof(Repository), Repository);
                info.AddValue(nameof(SourceRoot), SourceRoot);
            }

            public override string ToString() => $"GitFolderPath={GitFolderPath}";
        }
    }
}
