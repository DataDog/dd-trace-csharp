using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal class TraceContext : ITraceContext
    {
        private static readonly ILog Log = LogProvider.For<TraceContext>();

        private readonly object _lock = new object();
        private readonly List<Span> _spans = new List<Span>();

        private int _openSpans;
        private SamplingPriority? _samplingPriority;
        private bool _samplingPriorityLocked;

        public TraceContext(IDatadogTracer tracer)
        {
            Tracer = tracer;
        }

        public Span RootSpan { get; private set; }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public IDatadogTracer Tracer { get; }

        /// <summary>
        /// Gets or sets sampling priority.
        /// Once the sampling priority is locked with <see cref="LockSamplingPriority"/>,
        /// further attempts to set this are ignored.
        /// </summary>
        public SamplingPriority? SamplingPriority
        {
            get => _samplingPriority;
            set
            {
                if (!_samplingPriorityLocked)
                {
                    _samplingPriority = value;
                }
            }
        }

        public void AddSpan(Span span)
        {
            lock (_lock)
            {
                if (_openSpans == 0)
                {
                    // first span is the root span
                    RootSpan = span;

                    if (_samplingPriority == null)
                    {
                        if (span.Context.Parent is PropagationContext context && context.SamplingPriority != null)
                        {
                            // this is a root span created from a propagated context.
                            // lock sampling priority when a span is started from a propagated trace
                            _samplingPriority = context.SamplingPriority;
                            LockSamplingPriority();
                        }
                        else
                        {
                            // this is a local root span.
                            // determine an initial sampling priority for this trace, but don't lock it yet
                            string env = RootSpan.GetTag(Tags.Env);
                            _samplingPriority = Tracer.Sampler.GetSamplingPriority(RootSpan.ServiceName, env, RootSpan.Context.TraceId);
                        }
                    }
                }

                _spans.Add(span);
                _openSpans++;
            }
        }

        public void CloseSpan(Span span)
        {
            if (span == RootSpan)
            {
                // lock sampling priority and set metric when root span finishes
                LockSamplingPriority();

                if (_samplingPriority == null)
                {
                    Log.Warn("Cannot set span metric for sampling priority before it has been set.");
                }
                else
                {
                    span.SetMetric(Metrics.SamplingPriority, (int)_samplingPriority);
                }
            }

            lock (_lock)
            {
                _openSpans--;

                if (_openSpans == 0)
                {
                    Tracer.Write(_spans);
                }
            }
        }

        public void LockSamplingPriority()
        {
            if (_samplingPriority == null)
            {
                Log.Warn("Cannot lock sampling priority before it has been set.");
            }
            else
            {
                _samplingPriorityLocked = true;
            }
        }

        internal static ITraceContext GetTraceContext(IDatadogTracer tracer, ISpanContext parent)
        {
            ITraceContext traceContext;

            switch (parent)
            {
                case SpanContext context:
                    traceContext = context.TraceContext ?? new TraceContext(tracer);
                    break;
                case PropagationContext propagatedContext:
                    traceContext = new TraceContext(tracer)
                    {
                        SamplingPriority = propagatedContext.SamplingPriority
                    };
                    break;
                default:
                    throw new ArgumentException("Type of parent is not a supported.", nameof(parent));
            }

            return traceContext;
        }
    }
}
