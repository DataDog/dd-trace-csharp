// <copyright file="IBinding.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Defines an interface for a parameter binding.
    /// </summary>
    public interface IBinding
    {
        /// <summary>
        /// Gets a value indicating whether the binding was sourced from a parameter attribute.
        /// </summary>
        bool FromAttribute { get; }
    }
}
