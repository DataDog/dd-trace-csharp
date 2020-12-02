using System;
using Datadog.Trace.ClrProfiler.Integrations.StackExchange.Redis;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class RedisHelper
    {
        private const string OperationName = "redis.command";
        private const string ServiceName = "redis";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(RedisHelper));

        internal static Scope CreateScope(Tracer tracer, string integrationName, string host, string port, string rawCommand)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(integrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            string serviceName = $"{tracer.DefaultServiceName}-{ServiceName}";
            Scope scope = null;

            try
            {
                var tags = new RedisTags();

                scope = tracer.StartActiveWithTags(OperationName, serviceName: serviceName, tags: tags);
                int separatorIndex = rawCommand.IndexOf(' ');
                string command;

                if (separatorIndex >= 0)
                {
                    command = rawCommand.Substring(0, separatorIndex);
                }
                else
                {
                    command = rawCommand;
                }

                var span = scope.Span;
                span.Type = SpanTypes.Redis;
                span.ResourceName = command;
                tags.RawCommand = rawCommand;
                tags.Host = host;
                tags.Port = port;

                tags.Measured = 1;
                tags.SetAnalyticsSampleRate(integrationName, tracer.Settings, enabledWithGlobalSetting: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
