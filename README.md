# .NET Tracer for Datadog APM

## Installation and Usage

Please [read our documentation](https://docs.datadoghq.com/tracing/setup/dotnet) for instructions on setting up .NET tracing and details about supported frameworks.

## Downloads
Package|Download
-|-
Windows and Linux Installers|[See releases](https://github.com/DataDog/dd-trace-dotnet/releases)
`Datadog.Trace`|[![Datadog.Trace](https://img.shields.io/nuget/vpre/Datadog.Trace.svg)](https://www.nuget.org/packages/Datadog.Trace)
`Datadog.Trace.OpenTracing`|[![Datadog.Trace.OpenTracing](https://img.shields.io/nuget/vpre/Datadog.Trace.OpenTracing.svg)](https://www.nuget.org/packages/Datadog.Trace.OpenTracing)
`Datadog.Trace.ClrProfiler.Managed`|[![Datadog.Trace.ClrProfiler.Managed](https://img.shields.io/nuget/vpre/Datadog.Trace.ClrProfiler.Managed.svg)](https://www.nuget.org/packages/Datadog.Trace.ClrProfiler.Managed)

## Build Status

Pipeline                       | `develop`                                                                                                                                                                                                              | `master`
-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Windows C# unit tests          | [![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-unit-tests-managed?branchName=develop)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=1) |[![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-unit-tests-managed?branchName=master)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=1)
Windows C++ unit tests         | [![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-unit-tests-native?branchName=develop)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=11) |[![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-unit-tests-native?branchName=master)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=11)
Windows integration tests      | [![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-integration-tests?branchName=develop)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=5)  |[![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Windows/windows-integration-tests?branchName=master)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=5)
Linux C# unit tests            | [![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Linux/linux-unit-tests-managed?branchName=develop)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=2)     |[![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Linux/linux-unit-tests-managed?branchName=master)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=2)
Linux integration tests        | [![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Linux/linux-integration-tests?branchName=develop)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=13)     |[![Build Status](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_apis/build/status/Linux/linux-integration-tests?branchName=master)](https://dev.azure.com/datadog-apm/dd-trace-dotnet/_build?definitionId=13)

# Development

## Components

**[Datadog Agent](https://github.com/DataDog/datadog-agent)**: A service that runs on your application servers, accepting trace data from the Datadog Tracer and sending it to Datadog. The Agent is not part of this repo; it's the same Agent to which all Datadog tracers (e.g. Go, Python, Java, Ruby) send data.

**[Datadog .NET Tracer](https://github.com/DataDog/dd-trace-dotnet)**: This repository. A set of .NET libraries that let you trace any piece of your .NET code. Supports manual instrumentation and can automatically instrument supported libraries out-of-the-box.

## Windows

### Minimum requirements

- [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/) v15.7 or newer
  - Workloads
    - Desktop development with C++
    - .NET desktop development
    - .NET Core cross-platform development
    - Optional: ASP.NET and web development (to build samples)
  - Individual components
    - .NET Framework 4.7 targeting pack
- [.NET Core 2.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.1)
- Optional: [WiX Toolset 3.11.1](http://wixtoolset.org/releases/) or newer to build Windows installer (msi)
  - Requires .NET Framework 3.5 SP2 (install from Windows Features control panel: `OptionalFeatures.exe`)
  - [WiX Toolset VS2017 Extension](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension) to build installer from VS2017
- Optional: [Docker for Windows](https://docs.docker.com/docker-for-windows/) to build Linux binaries and run integration tests on Linux containers. See [section on Docker Compose](#building-and-running-tests-with-docker-compose).
  - Requires Windows 10 (1607 Anniversary Update, Build 14393 or newer)

Microsoft provides [evaluation developer VMs]((https://developer.microsoft.com/en-us/windows/downloads/virtual-machines)) with Windows 10 and Visual Studio 2017 pre-installed.

### Building from a command line

From a _Developer Command Prompt for VS 2017_:

```cmd
rem Restore NuGet packages
dotnet restore Datadog.Trace.sln

rem Build C# projects (Platform: always AnyCPU)
msbuild Datadog.Trace.proj /t:BuildCsharp /p:Configuration=Release;Platform=AnyCPU

rem Build NuGet packages
dotnet pack src/Datadog.Trace/Datadog.Trace.csproj
dotnet pack src/Datadog.Trace.OpenTracing/Datadog.Trace.OpenTracing.csproj
dotnet pack src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj

rem Build C++ projects (Platform: x64 or x86)
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=Release;Platform=x64

rem Build MSI installer (Platform: x64 or x86)
msbuild Datadog.Trace.proj /t:msi /p:Configuration=Release;Platform=x64
```

## Linux

### Minimum requirements

To build C# projects and NuGet packages only
- [.NET Core SDK 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)

To build everything and run integration tests
- [Docker Compose](https://docs.docker.com/compose/install/)

### Building and running tests with Docker Compose

You can use [Docker Compose](https://docs.docker.com/compose/) with Linux containers to build Linux binaries and run the test suites. This works on both Linux and Windows hosts.

```bash
# build C# projects
docker-compose run build

# build C++ project
docker-compose run Datadog.Trace.ClrProfiler.Native

# run integration tests
docker-compose run Datadog.Trace.ClrProfiler.IntegrationTests
```

## Further Reading

Datadog APM
- [Datadog APM](https://docs.datadoghq.com/tracing/)
- [Datadog APM - Tracing .NET Applications](https://docs.datadoghq.com/tracing/setup/dotnet/)
- [Datadog APM - Advanced Usage](https://docs.datadoghq.com/tracing/advanced_usage/?tab=dotnet)

Microsoft .NET Profiling APIs
- [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
- [Metadata API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/)
- [The Book of the Runtime - Profiling](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/profiling.md)

OpenTracing
- [OpenTracing documentation](https://github.com/opentracing/opentracing-csharp)
- [OpenTracing terminology](https://github.com/opentracing/specification/blob/master/specification.md)

## Get in touch

If you have questions, feedback, or feature requests, reach our [support](https://docs.datadoghq.com/help).
