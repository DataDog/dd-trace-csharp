﻿using System;
using OpenTracing;
using System.Collections.Generic;
using Datadog.Tracer.Logging;

namespace Datadog.Tracer
{
    public class SpanBuilder : ISpanBuilder
    {
        private static ILog _log = LogProvider.For<SpanBuilder>();

        private Object _lock = new Object();
        private IDatadogTracer _tracer;
        private string _operationName;
        private SpanContext _parent;
        private DateTimeOffset? _start;
        private Dictionary<string, string> _tags;
        private string _serviceName;

        internal SpanBuilder(IDatadogTracer tracer, string operationName)
        {
            _tracer = tracer;
            _operationName = operationName;
        }

        public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext)
        {
            lock (_lock)
            {
                if (referenceType == References.ChildOf)
                {
                    _parent = referencedContext as SpanContext;
                    return this;
                }
            }
            _log.Debug("ISpanBuilder.AddReference is not implemented for other references than ChildOf by Datadog.Tracer");
            return this;
        }

        public ISpanBuilder AsChildOf(ISpan parent)
        {
            lock (_lock)
            {
                _parent = parent.Context as SpanContext;
                return this;
            }
        }

        public ISpanBuilder AsChildOf(ISpanContext parent)
        {
            lock (_lock)
            {
                _parent = parent as SpanContext;
                return this;
            }
        }

        public ISpanBuilder FollowsFrom(ISpan parent)
        {
            _log.Debug("ISpanBuilder.FollowsFrom is not implemented by Datadog.Tracer");
            return this;
        }

        public ISpanBuilder FollowsFrom(ISpanContext parent)
        {
            _log.Debug("ISpanBuilder.FollowsFrom is not implemented by Datadog.Tracer");
            return this;
        }

        public ISpan Start()
        {
            lock (_lock)
            {
                var span = new Span(_tracer, _parent, _operationName, _serviceName, _start);
                span.TraceContext.AddSpan(span);
                if (_tags != null)
                {
                    foreach (var pair in _tags)
                    {
                        span.SetTag(pair.Key, pair.Value);
                    }
                }
                return span;
            }
        }

        public ISpanBuilder WithStartTimestamp(DateTimeOffset startTimestamp)
        {
            lock (_lock)
            {
                _start = startTimestamp;
                return this;
            }
        }

        public ISpanBuilder WithTag(string key, bool value)
        {
            return WithTag(key, value.ToString());
        }

        public ISpanBuilder WithTag(string key, double value)
        {
            return WithTag(key, value.ToString());
        }

        public ISpanBuilder WithTag(string key, int value)
        {
            return WithTag(key, value.ToString());
        }

        public ISpanBuilder WithTag(string key, string value)
        {
            lock (_lock)
            {
                if (key == Tags.ServiceName)
                {
                    _serviceName = value;
                    return this;
                }
                if (_tags == null)
                {
                    _tags = new Dictionary<string, string>();
                }
                _tags[key] = value;
                return this;
            }
        }
    }
}
