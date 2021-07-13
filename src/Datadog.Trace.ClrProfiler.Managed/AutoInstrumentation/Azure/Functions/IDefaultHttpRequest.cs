// <copyright file="IDefaultHttpRequest.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Interface for ducktyping
    /// </summary>
    public interface IDefaultHttpRequest
    {
        /// <summary>
        /// Gets the Host
        /// </summary>
        object Host { get; }

        /// <summary>
        /// Gets the Path
        /// </summary>
        object Path { get; }

        /// <summary>
        /// Gets the Method
        /// </summary>
        object Method { get; }

        /// <summary>
        /// Gets the request headers
        /// </summary>
        IRequestHeaders Headers { get; }
    }
}
