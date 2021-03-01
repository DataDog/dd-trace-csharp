#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Util;

namespace Datadog.Trace.ServiceFabric
{
    internal static class ServiceRemotingHelpers
    {
        public const string AssemblyName = "Microsoft.ServiceFabric.Services.Remoting";

        public const string ClientEventsTypeName = "Microsoft.ServiceFabric.Services.Remoting.V2.Client.ServiceRemotingClientEvents";

        public const string ServiceEventsTypeName = "Microsoft.ServiceFabric.Services.Remoting.V2.Runtime.ServiceRemotingServiceEvents";

        public const string SendRequestEventName = "SendRequest";

        public const string ReceiveResponseEventName = "ReceiveResponse";

        public const string ReceiveRequestEventName = "ReceiveRequest";

        public const string SendResponseEventName = "SendResponse";

        public static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(nameof(IntegrationIds.ServiceRemoting));

        private const string SpanNamePrefix = "service_remoting";

        private static readonly Logging.IDatadogLogger Log = Logging.DatadogLogging.GetLoggerFor(typeof(ServiceRemotingHelpers));

        private static readonly string ServiceFabricServiceName = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.ServiceName);

        private static readonly string ApplicationId = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.ApplicationId);

        private static readonly string ApplicationName = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.ApplicationName);

        private static readonly string PartitionId = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.PartitionId);

        private static readonly string NodeId = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.NodeId);

        private static readonly string NodeName = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableNames.NodeName);

        public static bool AddEventHandler(string typeName, string eventName, EventHandler eventHandler)
        {
            string fullEventName = $"{typeName}.{eventName}";

            try
            {
                Type? type = Type.GetType($"{typeName}, {AssemblyName}", throwOnError: false);

                if (type == null)
                {
                    Log.Warning("Could not get type {typeName}.", typeName);
                    return false;
                }

                EventInfo? eventInfo = type.GetEvent(eventName, BindingFlags.Static | BindingFlags.Public);

                if (eventInfo == null)
                {
                    Log.Warning("Could not get event {eventName}.", fullEventName);
                    return false;
                }

                // use null target because event is static
                eventInfo.AddEventHandler(target: null, eventHandler);
                Log.Debug("Subscribed to event {eventName}.", fullEventName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding event handler to {eventName}.", fullEventName);
                return false;
            }
        }

        public static void GetMessageHeaders(EventArgs? eventArgs, out IServiceRemotingRequestEventArgs? requestEventArgs, out IServiceRemotingRequestMessageHeader? messageHeaders)
        {
            requestEventArgs = null;
            messageHeaders = null;

            if (eventArgs == null)
            {
                Log.Warning("Unexpected null EventArgs.");
                return;
            }

            try
            {
                requestEventArgs = eventArgs.DuckAs<IServiceRemotingRequestEventArgs>();
                messageHeaders = requestEventArgs?.Request?.GetHeader();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error accessing request headers.");
                return;
            }

            if (messageHeaders == null)
            {
                Log.Warning("Cannot access request headers.");
            }
        }

        public static string GetSpanName(string spanKind)
        {
            return $"{SpanNamePrefix}.{spanKind}";
        }

        public static Span CreateSpan(
            Tracer tracer,
            ISpanContext? context,
            string spanKind,
            IServiceRemotingRequestEventArgs? eventArgs,
            IServiceRemotingRequestMessageHeader? messageHeader)
        {
            string? methodName = null;
            string? resourceName = null;
            string? serviceUrl = null;

            string serviceFabricServiceName = ServiceFabricServiceName;

            if (eventArgs != null)
            {
                methodName = eventArgs.MethodName ??
                             messageHeader?.MethodName ??
                             messageHeader?.MethodId.ToString(CultureInfo.InvariantCulture) ??
                             "unknown_method";

                serviceUrl = eventArgs.ServiceUri?.AbsoluteUri;
                resourceName = serviceUrl == null ? methodName : $"{serviceUrl}/{methodName}";
            }

            var tags = new ServiceRemotingTags(spanKind)
            {
                ApplicationId = ApplicationId,
                ApplicationName = ApplicationName,
                PartitionId = PartitionId,
                NodeId = NodeId,
                NodeName = NodeName,
                ServiceName = serviceFabricServiceName,
                RemotingUri = serviceUrl,
                RemotingMethodName = methodName
            };

            if (messageHeader != null)
            {
                tags.RemotingMethodId = messageHeader.MethodId.ToString(CultureInfo.InvariantCulture);
                tags.RemotingInterfaceId = messageHeader.InterfaceId.ToString(CultureInfo.InvariantCulture);
                tags.RemotingInvocationId = messageHeader.InvocationId;
            }

            Span span = tracer.StartSpan(GetSpanName(spanKind), tags, context);
            span.ResourceName = resourceName;

            switch (spanKind)
            {
                case SpanKinds.Client:
                    tags.SetAnalyticsSampleRate(IntegrationId, Tracer.Instance.Settings, enabledWithGlobalSetting: false);
                    break;
                case SpanKinds.Server:
                    tags.SetAnalyticsSampleRate(IntegrationId, Tracer.Instance.Settings, enabledWithGlobalSetting: true);
                    break;
            }

            return span;
        }

        public static void FinishSpan(EventArgs? e, string spanKind)
        {
            try
            {
                var scope = Tracer.Instance.ActiveScope;

                if (scope == null)
                {
                    Log.Warning("Expected an active scope, but there is none.");
                    return;
                }

                string expectedSpanName = GetSpanName(spanKind);

                if (expectedSpanName != scope.Span.OperationName)
                {
                    Log.Warning("Expected span name {expectedSpanName}, but found {actualSpanName} instead.", expectedSpanName, scope.Span.OperationName);
                    return;
                }

                try
                {
                    var eventArgs = e?.DuckAs<IServiceRemotingFailedResponseEventArgs>();
                    var exception = eventArgs?.Error;

                    if (exception != null)
                    {
                        scope.Span?.SetException(exception);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error setting exception tags on span.");
                }

                scope.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error accessing or finishing active span.");
            }
        }
    }
}
