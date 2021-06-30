// <copyright file="IFunctionBinding.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Interface for ducktyping
    /// </summary>
    public interface IFunctionBinding
    {
        // [Duck(Name = "_parameters", Kind = DuckKind.Field)]
        // IDictionary<string, object> Parameters { get; }
    }
}
