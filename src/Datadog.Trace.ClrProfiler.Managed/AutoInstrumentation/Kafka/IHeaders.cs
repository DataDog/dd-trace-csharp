﻿namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// Headers interface for duck-typing
    /// </summary>
    public interface IHeaders
    {
        /// <summary>
        /// Adds a header to the collection
        /// </summary>
        /// <param name="key">The header's key value</param>
        /// <param name="val">The value of the header. May be null. Format strings as UTF8</param>
        public void Add(string key, byte[] val);

        /// <summary>
        ///     Try to get the value of the latest header with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key to get the associated value of.
        /// </param>
        /// <param name="lastHeader">
        ///     The value of the latest element in the collection with the
        ///     specified key, if a header with that key was present in the
        ///     collection.
        /// </param>
        /// <returns>
        ///     true if the a value with the specified key was present in
        ///     the collection, false otherwise.
        /// </returns>
        public bool TryGetLastBytes(string key, out byte[] lastHeader);
    }
}
