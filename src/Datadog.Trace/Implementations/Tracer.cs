﻿using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;
using OpenTracing;
using OpenTracing.Propagation;

namespace Datadog.Trace
{
    internal class Tracer : ITracer
    {
        private static readonly ILog _log = LogProvider.For<Tracer>();

        private readonly TracerBase _tracer;
        private readonly Dictionary<string, ICodec> _codecs;

        public Tracer(TracerBase tracer)
        {
            _tracer = tracer;
            _codecs = new Dictionary<string, ICodec> { { Formats.HttpHeaders.Name, new HttpHeadersCodec(_tracer) } };
        }

        public Tracer(IAgentWriter agentWriter, List<ServiceInfo> serviceInfo = null, string defaultServiceName = null, bool isDebugEnabled = false)
        {
            _tracer = new TracerBase(agentWriter, serviceInfo, defaultServiceName, isDebugEnabled);
            _codecs = new Dictionary<string, ICodec> { { Formats.HttpHeaders.Name, new HttpHeadersCodec(_tracer) } };
        }

        public ISpanBuilder BuildSpan(string operationName)
        {
            return new SpanBuilder(_tracer, operationName);
        }

        public ISpanContext Extract<TCarrier>(Format<TCarrier> format, TCarrier carrier)
        {
            _codecs.TryGetValue(format.Name, out ICodec codec);
            if (codec != null)
            {
                return codec.Extract(carrier);
            }
            else
            {
                _log.Error($"Tracer.Extract is not implemented for {format.Name} by Datadog.Trace");
                throw new UnsupportedFormatException();
            }
        }

        public void Inject<TCarrier>(ISpanContext spanContext, Format<TCarrier> format, TCarrier carrier)
        {
            _codecs.TryGetValue(format.Name, out ICodec codec);
            if (codec != null)
            {
                var ddSpanContext = spanContext as SpanContext;
                if (ddSpanContext == null)
                {
                    throw new ArgumentException("Inject should be called with a Datadog.Trace.SpanContext argument");
                }

                codec.Inject(ddSpanContext, carrier);
            }
            else
            {
                _log.Error($"Tracer.Inject is not implemented for {format.Name} by Datadog.Trace");
                throw new UnsupportedFormatException();
            }
        }
    }
}
