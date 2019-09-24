#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/.."

PUBLISH_OUTPUT="$( pwd )/src/bin/managed-publish"
mkdir -p "$PUBLISH_OUTPUT/netstandard2.0"

dotnet build -c $buildConfiguration src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj

for proj in Datadog.Trace Datadog.Trace.OpenTracing ; do
    dotnet publish -f netstandard2.0 -c $buildConfiguration src/$proj/$proj.csproj
done

dotnet publish -f netstandard2.0 -c $buildConfiguration src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj -o "$PUBLISH_OUTPUT/netstandard2.0"

for sample in Samples.AspNetCoreMvc2 Samples.Elasticsearch Samples.Elasticsearch.V5 Samples.ServiceStack.Redis Samples.StackExchange.Redis Samples.SqlServer Samples.MongoDB Samples.HttpMessageHandler Samples.Npgsql Samples.GraphQL ; do
    dotnet publish -f netcoreapp2.1 -c $buildConfiguration samples/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
    # rm -r -f samples/$sample/obj

    dotnet publish -f netcoreapp3.0 -c $buildConfiguration samples/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
    # rm -r -f samples/$sample/obj
done

for sample in OrleansCrash DataDogThreadTest HttpMessageHandler.StackOverflowException StackExchange.Redis.StackOverflowException AspNetMvcCorePerformance AssemblyLoad.FileNotFoundException TraceContext.InvalidOperationException ; do
    dotnet publish -f netcoreapp2.1 -c $buildConfiguration reproductions/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
    dotnet publish -f netcoreapp3.0 -c $buildConfiguration reproductions/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
done

# Clean samples/Samples.AspNetCoreMvc2 because it does code generation based on target framework
# rm -r -f samples/Samples.AspNetCoreMvc2/obj
dotnet msbuild Datadog.Trace.proj -t:RestoreAndBuildSamplesForPackageVersions -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT" -p:TargetFramework=netcoreapp2.1

# Clean samples/Samples.AspNetCoreMvc2 because it does code generation based on target framework
# rm -r -f samples/Samples.AspNetCoreMvc2/obj
dotnet msbuild Datadog.Trace.proj -t:RestoreAndBuildSamplesForPackageVersions -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT" -p:TargetFramework=netcoreapp3.0

for proj in Datadog.Trace.ClrProfiler.IntegrationTests ; do
    dotnet publish -f netcoreapp2.1 -c $buildConfiguration test/$proj/$proj.csproj
    dotnet publish -f netcoreapp3.0 -c $buildConfiguration test/$proj/$proj.csproj
done
