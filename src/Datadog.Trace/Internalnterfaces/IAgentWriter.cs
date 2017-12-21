﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datadog.Trace
{
    internal interface IAgentWriter
    {
        void WriteTrace(List<SpanBase> trace);

        void WriteServiceInfo(ServiceInfo serviceInfo);

        Task FlushAndCloseAsync();
    }
}
