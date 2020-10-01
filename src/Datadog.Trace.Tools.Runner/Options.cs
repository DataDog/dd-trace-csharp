using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Datadog.Trace.Tools.Runner
{
    internal class Options
    {
        [Usage(ApplicationAlias = "dd-trace")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Set CI environment variables", new Options { SetEnvironmentVariables = true });
                yield return new Example("Wrap a command with the CLR profiler environment variables", new Options { Value = new string[] { "dotnet", "test" } });
                yield return new Example("Wrap a command with the CLR profiler and change the datadog environment key", new Options { Environment = "ci", Value = new string[] { "dotnet", "test" } });
                yield return new Example("Wrap a command with the CLR profiler and changing the datadog agent url", new Options { AgentUrl = "http://agent:8126", Value = new string[] { "dotnet", "test" } });
            }
        }

        [Option("set-ci", Required = false, Default = false, HelpText = "Setup the clr profiler environment variables for the CI job. (only supported in Azure Pipelines)")]
        public bool SetEnvironmentVariables { get; set; }

        [Option("env", Required = false, HelpText = "Environment name.")]
        public string Environment { get; set; }

        [Option("agent-url", Required = false, HelpText = "Datadog trace agent url.")]
        public string AgentUrl { get; set; }

        [Option("tracer-home", Required = false, HelpText = "Sets the tracer home folder path.")]
        public string TracerHomeFolder { get; set; }

        [Value(0, Hidden = true, HelpText = "Command to be wrapped by the cli tool.")]
        public IEnumerable<string> Value { get; set; }
    }
}
