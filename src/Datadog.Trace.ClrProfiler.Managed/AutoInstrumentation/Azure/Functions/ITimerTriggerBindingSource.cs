// <copyright file="ITimerTriggerBindingSource.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Interface for ducktyping
    /// </summary>
    public interface ITimerTriggerBindingSource
    {
        /// <summary>
        /// Gets the parameters field
        /// </summary>
        [DuckField("_parameters")]
        IDictionary<string, object> Parameters { get; }
    }
}
