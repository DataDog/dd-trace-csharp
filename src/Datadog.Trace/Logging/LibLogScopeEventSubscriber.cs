using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Datadog.Trace.Logging.LogProviders;

namespace Datadog.Trace.Logging
{
    internal class LibLogScopeEventSubscriber : IDisposable
    {
        private readonly IScopeManager _scopeManager;

        // Each mapped context sets a key-value pair into the logging context
        // Disposing the context unsets the key-value pair
        //
        // IMPORTANT: The contexts must be closed in reverse-order of opening,
        //            so by convention always open the TraceId context before
        //            opening the SpanId context, and close the contexts in
        //            the opposite order
        private readonly ConcurrentStack<IDisposable> _contextDisposalStack = new ConcurrentStack<IDisposable>();

        public LibLogScopeEventSubscriber(IScopeManager scopeManager)
        {
            _scopeManager = scopeManager;

            var logProvider = LogProvider.CurrentLogProvider ?? LogProvider.ResolveLogProvider();
            if (logProvider is SerilogLogProvider)
            {
                _scopeManager.SpanOpened += SerilogOnSpanOpened;
                _scopeManager.SpanClosed += SerilogOnSpanClosed;
            }
            else
            {
                _scopeManager.SpanActivated += OnSpanActivated;
                _scopeManager.TraceEnded += OnTraceEnded;
            }
        }

        public void SerilogOnSpanOpened(object sender, SpanEventArgs spanEventArgs)
        {
            SetLoggingValues(spanEventArgs.Span.TraceId, spanEventArgs.Span.SpanId);
        }

        public void SerilogOnSpanClosed(object sender, SpanEventArgs spanEventArgs)
        {
            DisposeLastPair();
        }

        public void OnSpanActivated(object sender, SpanEventArgs spanEventArgs)
        {
            DisposeAll();
            SetLoggingValues(spanEventArgs.Span.TraceId, spanEventArgs.Span.SpanId);
        }

        public void OnTraceEnded(object sender, SpanEventArgs spanEventArgs)
        {
            DisposeAll();
        }

        public void Dispose()
        {
            _scopeManager.SpanActivated -= OnSpanActivated;
            _scopeManager.TraceEnded -= OnTraceEnded;
            DisposeAll();
        }

        private void DisposeLastPair()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_contextDisposalStack.TryPop(out IDisposable ctxDisposable))
                {
                    ctxDisposable.Dispose();
                }
                else
                {
                    // There is nothing left to pop so do nothing.
                    // Though we are in a strange circumstance if we did not balance
                    // the stack properly
                    Debug.Fail($"{nameof(DisposeLastPair)} call failed. Too few items on the context stack.");
                }
            }
        }

        private void DisposeAll()
        {
            while (_contextDisposalStack.TryPop(out IDisposable ctxDisposable))
            {
                ctxDisposable.Dispose();
            }
        }

        private void SetLoggingValues(ulong traceId, ulong spanId)
        {
            _contextDisposalStack.Push(
                LogProvider.OpenMappedContext(
                    CorrelationIdentifier.TraceIdKey, traceId, destructure: false));
            _contextDisposalStack.Push(
                LogProvider.OpenMappedContext(
                    CorrelationIdentifier.SpanIdKey, spanId, destructure: false));
        }
    }
}
