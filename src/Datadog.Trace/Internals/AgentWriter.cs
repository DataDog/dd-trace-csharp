﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal class AgentWriter : IAgentWriter
    {
        private static ILog _log = LogProvider.For<AgentWriter>();

        private readonly AgentWriterBuffer<List<SpanBase>> _tracesBuffer = new AgentWriterBuffer<List<SpanBase>>(1000);
        private readonly AgentWriterBuffer<ServiceInfo> _servicesBuffer = new AgentWriterBuffer<ServiceInfo>(100);
        private readonly IApi _api;
        private readonly Task _flushTask;
        private readonly TaskCompletionSource<bool> _processExit = new TaskCompletionSource<bool>();

        public AgentWriter(IApi api)
        {
            _api = api;
            _flushTask = Task.Run(FlushTracesTaskLoop);
        }

        public void WriteServiceInfo(ServiceInfo serviceInfo)
        {
            var success = _servicesBuffer.Push(serviceInfo);
            if (!success)
            {
                _log.Debug("ServiceInfo buffer is full, dropping it.");
            }
        }

        public void WriteTrace(List<SpanBase> trace)
        {
            var success = _tracesBuffer.Push(trace);
            if (!success)
            {
                _log.Debug("Trace buffer is full, dropping it.");
            }
        }

        public async Task FlushAndCloseAsync()
        {
            _processExit.SetResult(true);
            await Task.WhenAny(_flushTask, Task.Delay(TimeSpan.FromSeconds(2)));
            if (!_flushTask.IsCompleted)
            {
                _log.Warn("Could not flush all traces before process exit");
            }
        }

        private async Task FlushTracesAsync()
        {
            var traces = _tracesBuffer.Pop();
            if (traces.Any())
            {
                await _api.SendTracesAsync(traces);
            }

            var services = _servicesBuffer.Pop();
            if (services.Any())
            {
                // TODO:bertrand batch these calls
                await Task.WhenAll(services.Select(_api.SendServiceAsync));
            }
        }

        private async Task FlushTracesTaskLoop()
        {
            while (true)
            {
                try
                {
                    await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), _processExit.Task);
                    if (_processExit.Task.IsCompleted)
                    {
                        await FlushTracesAsync();
                        return;
                    }
                    else
                    {
                        await FlushTracesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _log.ErrorException("An unhandled error occurred during the flushing task", ex);
                }
            }
        }
    }
}
