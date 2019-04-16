#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

$DIR/with-profiler-logs.bash \
    wait-for-it redis:6379 -- \
    wait-for-it elasticsearch:9200 -- \
    wait-for-it sqlserver:1433 -- \
    wait-for-it mongo:27017 -- \
    dotnet test --verbosity minimal $DIR/../test/Datadog.Trace.ClrProfiler.IntegrationTests/Datadog.Trace.ClrProfiler.IntegrationTests.csproj
