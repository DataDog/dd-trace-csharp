using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// RabbitMQ.Client BasicDeliver calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        Assembly = "RabbitMQ.Client",
        Type = "RabbitMQ.Client.Events.EventingBasicConsumer",
        Method = "HandleBasicDeliver",
        ReturnTypeName = ClrNames.Void,
        ParametersTypesNames = new[] { ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, RabbitMQConstants.IBasicPropertiesTypeName, ClrNames.Ignore },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        IntegrationName = RabbitMQConstants.IntegrationName)]
    public class BasicDeliverIntegration
    {
        private const string Command = "basic.deliver";
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(BasicPublishIntegration));

        private static Func<IDictionary<string, object>, string, IEnumerable<string>> headersGetter = ((carrier, key) =>
        {
            if (carrier.TryGetValue(key, out object value) && value is byte[] bytes)
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        });

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBasicProperties">Type of the message properties</typeparam>
        /// <typeparam name="TBody">Type of the message body</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="consumerTag">The original consumerTag argument</param>
        /// <param name="deliveryTag">The original deliveryTag argument</param>
        /// <param name="redelivered">The original redelivered argument</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(TTarget instance, string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, TBasicProperties basicProperties, TBody body)
            where TBasicProperties : IBasicProperties
            where TBody : IBody // ReadOnlyMemory<byte> body in 6.0.0
        {
            SpanContext propagatedContext = null;

            // try to extract propagated context values from headers
            if (basicProperties?.Headers != null)
            {
                try
                {
                    propagatedContext = SpanContextPropagator.Instance.Extract(basicProperties.Headers, headersGetter);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error extracting propagated headers.");
                }
            }

            var scope = ScopeFactory.CreateRabbitMQScope(Tracer.Instance, out RabbitMQTags tags, Command, parentContext: propagatedContext, spanKind: SpanKinds.Consumer, exchange: exchange, routingKey: routingKey);
            if (tags != null)
            {
                tags.MessageSize = body?.Length.ToString() ?? "0";
            }

            return new CallTargetState(scope);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            state.Scope.DisposeWithException(exception);
            return CallTargetReturn.GetDefault();
        }
    }
}
