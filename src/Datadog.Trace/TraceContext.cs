using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Datadog.Trace
{
    internal class TraceContext : ITraceContext
    {
        private static readonly Vendoring.Serilog.ILogger Log = Vendoring.DatadogLogging.For<TraceContext>();

        private readonly object _lock = new object();
        private readonly DateTimeOffset _utcStart = DateTimeOffset.UtcNow;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private List<Span> _spans = new List<Span>();
        private int _openSpans;
        private SamplingPriority? _samplingPriority;
        private bool _samplingPriorityLocked;

        public TraceContext(IDatadogTracer tracer)
        {
            Tracer = tracer;
        }

        public Span RootSpan { get; private set; }

        public DateTimeOffset UtcNow => _utcStart.Add(_stopwatch.Elapsed);

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
                if (RootSpan == null)
                {
                    // first span added is the root span
                    RootSpan = span;

                    if (_samplingPriority == null)
                    {
                        if (span.Context.Parent is SpanContext context && context.SamplingPriority != null)
                        {
                            // this is a root span created from a propagated context that contains a sampling priority.
                            // lock sampling priority when a span is started from a propagated trace.
                            _samplingPriority = context.SamplingPriority;
                            LockSamplingPriority();
                        }
                        else
                        {
                            // this is a local root span (i.e. not propagated).
                            string env = RootSpan.GetTag(Tags.Env);

                            // determine an initial sampling priority for this trace, but don't lock it yet
                            _samplingPriority =
                                Tracer.Sampler?.GetSamplingPriority(RootSpan.ServiceName, env, RootSpan.Context.TraceId);
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
                    Log.Warning("Cannot set span metric for sampling priority before it has been set.");
                }
                else
                {
                    span.SetMetric(Metrics.SamplingPriority, (int)_samplingPriority);
                }
            }

            List<Span> spansToWrite = null;

            lock (_lock)
            {
                _openSpans--;

                if (_openSpans == 0)
                {
                    spansToWrite = _spans;
                    _spans = new List<Span>();
                }
            }

            if (spansToWrite != null)
            {
                Tracer.Write(spansToWrite);
            }
        }

        public void LockSamplingPriority()
        {
            if (_samplingPriority == null)
            {
                Log.Warning("Cannot lock sampling priority before it has been set.");
            }
            else
            {
                _samplingPriorityLocked = true;
            }
        }
    }
}
