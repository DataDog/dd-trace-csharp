﻿using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;
using OpenTracing;

namespace Datadog.Trace
{
    // TODO:bertrand this class should not be public
    internal class Span : ISpan
    {
        private static ILog _log = LogProvider.For<Span>();

        private Scope _scope;

        internal Span(Scope scope)
        {
            _scope = scope;
        }

        public ISpanContext Context => _scope.Span.Context;

        // This is only exposed for tests
        internal SpanBase DDSpan => _scope.Span;

        public string GetBaggageItem(string key)
        {
            _log.Debug("ISpan.GetBaggageItem is not implemented by Datadog.Trace");
            return null;
        }

        public ISpan Log(IEnumerable<KeyValuePair<string, object>> fields)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(string eventName)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, string eventName)
        {
            _log.Debug("ISpan.Log is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan SetBaggageItem(string key, string value)
        {
            _log.Debug("ISpan.SetBaggageItem is not implemented by Datadog.Trace");
            return this;
        }

        public ISpan SetOperationName(string operationName)
        {
            _scope.Span.OperationName = operationName;
            return this;
        }

        public ISpan SetTag(string key, bool value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, double value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, int value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, string value)
        {
            // TODO:bertrand do we want this behavior on the Span object too ?
            switch (key)
            {
                case DDTags.ResourceName:
                    _scope.Span.ResourceName = value;
                    return this;
                case Tags.Error:
                    _scope.Span.Error = value == "True";
                    return this;
                case DDTags.SpanType:
                    _scope.Span.Type = value;
                    return this;
            }

            _scope.Span.SetTag(key, value);
            return this;
        }

        public void Finish()
        {
            _scope.Span.Finish();
            _scope.Dispose();
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            _scope.Span.Finish(finishTimestamp);
            _scope.Dispose();
        }

        public void Dispose()
        {
            _scope.Span.Finish();
            _scope.Dispose();
        }
    }
}