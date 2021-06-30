// <copyright file="IBindingSource.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Interface for ducktyping
    /// </summary>
    public interface IBindingSource
    {
        /// <summary>
        /// Gets the binding field
        /// </summary>
        [Duck(Name = "_binding", Kind = DuckKind.Field)]
        object Binding { get; }

        /// <summary>
        /// Gets the parameters field
        /// </summary>
        [Duck(Name = "_parameters", Kind = DuckKind.Field)]
        IDictionary<string, object> Parameters { get; }
    }
}
