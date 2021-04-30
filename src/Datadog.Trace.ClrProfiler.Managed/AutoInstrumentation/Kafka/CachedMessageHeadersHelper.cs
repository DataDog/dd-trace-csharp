﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    internal static class CachedMessageHeadersHelper<TMarkerType>
    {
        private static readonly Func<object> _activator;

        static CachedMessageHeadersHelper()
        {
            var headersType = typeof(TMarkerType).Assembly.GetType("Confluent.Kafka.Headers");

            ConstructorInfo ctor = headersType.GetConstructor(System.Type.EmptyTypes);

            DynamicMethod createHeadersMethod = new DynamicMethod(
                $"KafkaCachedMessageHeadersHelpers",
                headersType,
                null,
                typeof(DuckType).Module,
                true);

            ILGenerator il = createHeadersMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            _activator = (Func<object>)createHeadersMethod.CreateDelegate(typeof(Func<object>));
        }

        /// <summary>
        /// Creates a Confluent.Kafka.Headers object and assigns it to an `IMessage` proxy
        /// </summary>
        /// <param name="messageProxy">The proxied Confluent.Kafka.Message{T1,T2} type</param>
        /// <returns>A proxy for the new Headers object</returns>
        public static IHeaders CreateAndSetHeaders<TMessageProxy>(TMessageProxy messageProxy)
            where TMessageProxy : IMessage
        {
            var headers = _activator();
            var headersProxy = headers.DuckCast<IHeaders>();
            messageProxy.Headers = headersProxy;
            return headersProxy;
        }
    }
}
