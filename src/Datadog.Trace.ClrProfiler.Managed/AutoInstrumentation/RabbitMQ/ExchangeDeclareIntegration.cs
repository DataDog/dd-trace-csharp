using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// RabbitMQ.Client ExchangeDeclare calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "RabbitMQ.Client",
        TypeName = "RabbitMQ.Client.Framing.Impl.Model",
        MethodName = "_Private_ExchangeDeclare",
        ReturnTypeName = ClrNames.Void,
        ParameterTypeNames = new[] { ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, RabbitMQConstants.IDictionaryArgumentsTypeName },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        IntegrationName = RabbitMQConstants.IntegrationName)]
    public class ExchangeDeclareIntegration
    {
        private const string Command = "exchange.declare";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="type">Type of the exchange.</param>
        /// <param name="passive">The original passive setting</param>
        /// <param name="durable">The original duable setting</param>
        /// <param name="autoDelete">The original autoDelete setting</param>
        /// <param name="internal">The original internal setting</param>
        /// <param name="nowait">The original nowait setting</param>
        /// <param name="arguments">The original arguments setting</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, string exchange, string type, bool passive, bool durable, bool autoDelete, bool @internal, bool nowait, IDictionary<string, object> arguments)
        {
            return new CallTargetState(ScopeFactory.CreateRabbitMQScope(Tracer.Instance, out _, Command, exchange: exchange));
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
