﻿using Datadog.Trace.DuckTyping;
// ReSharper disable SA1310

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// Partition for duck-typing
    /// </summary>
    [DuckCopy]
    public struct Offset
    {
        private const long RdKafkaOffsetBeginning = -2;
        private const long RdKafkaOffsetEnd = -1;
        private const long RdKafkaOffsetStored = -1000;
        private const long RdKafkaOffsetInvalid = -1001;

        /// <summary>
        /// Gets the long value corresponding to this offset
        /// </summary>
        public long Value;

        /// <summary>
        /// Based on the original implementation
        /// https://github.com/confluentinc/confluent-kafka-dotnet/blob/643c8fdc90f54f4d82d5135ae7e91a995f0efdee/src/Confluent.Kafka/Offset.cs#L274
        /// </summary>
        /// <returns>A string that represents the Offset object</returns>
        public override string ToString()
        {
            return Value switch
            {
                RdKafkaOffsetBeginning => $"Beginning [{RdKafkaOffsetBeginning}]",
                RdKafkaOffsetEnd => $"End [{RdKafkaOffsetEnd}]",
                RdKafkaOffsetStored => $"Stored [{RdKafkaOffsetStored}]",
                RdKafkaOffsetInvalid => $"Unset [{RdKafkaOffsetInvalid}]",
                _ => Value.ToString()
            };
        }
    }
}
