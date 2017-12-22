﻿using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    public class TracerBase : IDatadogTracer
    {
        private static readonly ILog _log = LogProvider.For<TracerBase>();

        private AsyncLocalScopeManager _scopeManager;
        private string _defaultServiceName;
        private Dictionary<string, ServiceInfo> _services = new Dictionary<string, ServiceInfo>();
        private IAgentWriter _agentWriter;
        private bool _isDebugEnabled;

        internal TracerBase(IAgentWriter agentWriter, List<ServiceInfo> serviceInfo = null, string defaultServiceName = null, bool isDebugEnabled = false)
        {
            _isDebugEnabled = isDebugEnabled;
            _agentWriter = agentWriter;
            _defaultServiceName = GetAppDomainFriendlyName() ?? Constants.UnkownService;
            if (serviceInfo != null)
            {
                foreach (var service in serviceInfo)
                {
                    _services[service.ServiceName] = service;
                }
            }

            foreach (var service in _services.Values)
            {
                _agentWriter.WriteServiceInfo(service);
            }

            // Register callbacks to make sure we flush the traces before exiting
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.CancelKeyPress += Console_CancelKeyPress;
            _scopeManager = new AsyncLocalScopeManager();
        }

        bool IDatadogTracer.IsDebugEnabled => _isDebugEnabled;

        string IDatadogTracer.DefaultServiceName => _defaultServiceName;

        AsyncLocalScopeManager IDatadogTracer.ScopeManager => _scopeManager;

        public SpanBase StartActive(SpanContext parent, string operationName, string serviceName = null, DateTime? startTime = null, bool ignoreActiveScope = false)
        {
            // TODO: honor the ignore active span
            // TODO: ajouter au span la possibilité de se désactiver comme ça le scope peut être construit on-demand par dessus
            // Genre Span.AsScope()
            // TODO test ignore active scope
            if (parent == null && !ignoreActiveScope)
            {
                parent = _scopeManager.Active.Context;
            }

            var span = new SpanBase(this, parent, operationName, serviceName, startTime);
            span.TraceContext.AddSpan(span);
            _scopeManager.Activate(span);
            return span;
        }

        public SpanBase StartManual(SpanContext parent, string operationName, string serviceName = null, DateTime? startTime = null, bool ignoreActiveScope = false)
        {
            var span = new SpanBase(this, parent, operationName, serviceName, startTime);
            span.TraceContext.AddSpan(span);
            return span;
        }

        void IDatadogTracer.Write(List<SpanBase> trace)
        {
            _agentWriter.WriteTrace(trace);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _agentWriter.FlushAndCloseAsync().Wait();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _agentWriter.FlushAndCloseAsync().Wait();
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _agentWriter.FlushAndCloseAsync().Wait();
        }

        private string GetAppDomainFriendlyName()
        {
            try
            {
                return AppDomain.CurrentDomain.FriendlyName;
            }
            catch
            {
                return null;
            }
        }
    }
}