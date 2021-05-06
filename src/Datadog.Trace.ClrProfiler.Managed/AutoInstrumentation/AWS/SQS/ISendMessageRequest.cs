﻿using System.Collections;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// SendMessageRequest interface for ducktyping
    /// </summary>
    public interface ISendMessageRequest
    {
        /// <summary>
        /// Gets the URL of the queue
        /// </summary>
        string QueueUrl { get; }

        /// <summary>
        /// Gets the message attributes
        /// </summary>
        IDictionary MessageAttributes { get; }
    }
}
