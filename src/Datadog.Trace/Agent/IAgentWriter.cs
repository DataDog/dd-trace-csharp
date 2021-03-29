using System;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IAgentWriter : IDisposable
    {
        void WriteTrace(Span[] trace);

        Task<bool> Ping();

        Task FlushTracesAsync();

        Task FlushAndCloseAsync();
    }
}
