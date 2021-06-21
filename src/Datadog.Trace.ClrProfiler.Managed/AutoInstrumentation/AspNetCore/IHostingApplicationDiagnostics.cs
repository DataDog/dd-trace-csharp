// <copyright file="IHostingApplicationDiagnostics.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNetCore
{
    /// <summary>
    /// HostingApplicationDiagnostics interface for ducktyping
    /// </summary>
    public interface IHostingApplicationDiagnostics
    {
        /// <summary>
        /// Gets the logger field
        /// </summary>
        [Duck(Name = "_logger", Kind = DuckKind.Field)]
        ILogger Logger { get; }
    }
}
