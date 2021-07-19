// <copyright file="AgentWriter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;
using Datadog.Trace.Agent;
using Datadog.Trace.AppSec.EventModel;
using Datadog.Trace.AppSec.EventModel.Batch;
using Datadog.Trace.AppSec.Transports;
using Datadog.Trace.Logging;

namespace Datadog.Trace.AppSec.Agent
{
    internal class AgentWriter : IAgentWriter
    {
        private const int BatchInterval = 2000;
        private const int MaxItemsPerBatch = 1000;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<AgentWriter>();
        private readonly ConcurrentQueue<IEvent> events;
        private readonly Task _periodicFlush;
        private readonly Sender _sender;

        internal AgentWriter()
        {
            events = new ConcurrentQueue<IEvent>();
            _periodicFlush = Task.Factory.StartNew(FlushTracesAsync, TaskCreationOptions.LongRunning);
            _periodicFlush.ContinueWith(t => Log.Error(t.Exception, "Error in sending appsec events"), TaskContinuationOptions.OnlyOnFaulted);
            _sender = new Sender();
        }

        public void AddEvent(IEvent @event)
        {
            Log.Warning($"add event to {events.Count} events");
            events.Enqueue(@event);
        }

        private async Task FlushTracesAsync()
        {
            while (true)
            {
                try
                {
                    if (events.Count == 0)
                    {
                        continue;
                    }

                    var appsecEvents = new List<IEvent>();
                    while (events.TryDequeue(out var result))
                    {
                        appsecEvents.Add(result);
                        Log.Warning($"Appsec events are now {appsecEvents.Count}");
                        if (appsecEvents.Count > MaxItemsPerBatch)
                        {
                            break;
                        }
                    }

                    await _sender.Send(appsecEvents);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occured in sending appsec events");
                }
                finally
                {
                    Thread.Sleep(BatchInterval);
                }
            }
        }
    }
}
