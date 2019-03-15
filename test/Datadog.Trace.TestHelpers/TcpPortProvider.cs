using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Datadog.Trace.TestHelpers
{
    /// <summary>
    /// Helper class that tries to provide unique ports numbers across processes and threads in the same machine.
    /// Used avoid port conflicts in concurrent tests that use the Agent, IIS, HttpListener, HttpClient, etc.
    /// This class cannot guarantee a port is actually available, but should help avoid most conflicts.
    /// </summary>
    public static class TcpPortProvider
    {
        private static readonly ConcurrentBag<int> ReturnedPorts = new ConcurrentBag<int>();

        private static readonly int MinPort = GetStartingPort();

        public static int GetOpenPort()
        {
            var usedPorts = GetUsedPorts();

            for (int port = MinPort; port < ushort.MaxValue; port++)
            {
                if (!ReturnedPorts.Contains(port) && !usedPorts.Contains(port))
                {
                    // don't return a port that was previously returned,
                    // even if it is not in use (it could still be used
                    // by the code that is was returned to)
                    ReturnedPorts.Add(port);
                    return port;
                }
            }

            throw new Exception("No open TCP port found.");
        }

        public static HashSet<int> GetUsedPorts()
        {
            var usedPorts = IPGlobalProperties.GetIPGlobalProperties()
                                              .GetActiveTcpListeners()
                                              .Select(ipEndPoint => ipEndPoint.Port);

            return new HashSet<int>(usedPorts);
        }

        private static int GetStartingPort()
        {
            // pick a starting port from the ephemeral port range (49152 – 65535) based on process id
            return (Process.GetCurrentProcess().Id % (65535 - 49152)) + 49152;
        }
    }
}
