// <copyright file="IHeaderDictionary.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Azure.Functions
{
    /// <summary>
    /// Represents HttpRequest and HttpResponse headers
    /// </summary>
    public interface IHeaderDictionary
    {
        /// <summary>
        /// Get header values for key
        /// </summary>
        IEnumerable<string> GetValues(string name);

        /// <summary>
        /// Try get header values for key
        /// </summary>
        bool TryGetValues(string name, out IEnumerable<string> values);
    }
}
