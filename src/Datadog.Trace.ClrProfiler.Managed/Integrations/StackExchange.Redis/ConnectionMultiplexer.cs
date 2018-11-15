using System;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.Integrations.StackExchange.Redis
{
    /// <summary>
    /// Wraps calls to the StackExchange redis library.
    /// </summary>
    public class ConnectionMultiplexer : Base
    {
        /// <summary>
        /// Execute a synchronous redis operation.
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="multiplexer">The connection multiplexer running the command.</param>
        /// <param name="message">The message to send to redis.</param>
        /// <param name="processor">The processor to handle the result.</param>
        /// <param name="server">The server to call.</param>
        /// <returns>The result</returns>
        public static T ExecuteSyncImpl<T>(object multiplexer, object message, object processor, object server)
        {
            var resultType = typeof(T);
            var multiplexerType = multiplexer.GetType();
            var asm = multiplexerType.Assembly;
            var messageType = asm.GetType("StackExchange.Redis.Message");
            var processorType = asm.GetType("StackExchange.Redis.ResultProcessor`1").MakeGenericType(resultType);
            var serverType = asm.GetType("StackExchange.Redis.ServerEndPoint");

            var originalMethod = DynamicMethodBuilder<Func<object, object, object, object, T>>
               .CreateMethodCallDelegate(
                    multiplexerType,
                    "ExecuteSyncImpl",
                    new[] { messageType, processorType, serverType },
                    new[] { resultType });

            using (var scope = CreateScope(multiplexer, message, server))
            {
                return originalMethod(multiplexer, message, processor, server);
            }
        }

        /// <summary>
        /// Execute an asynchronous redis operation.
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="multiplexer">The connection multiplexer running the command.</param>
        /// <param name="message">The message to send to redis.</param>
        /// <param name="processor">The processor to handle the result.</param>
        /// <param name="state">The state to use for the task.</param>
        /// <param name="server">The server to call.</param>
        /// <returns>An asynchronous task.</returns>
        public static object ExecuteAsyncImpl<T>(object multiplexer, object message, object processor, object state, object server)
        {
            var genericType = typeof(T);
            var multiplexerType = multiplexer.GetType();
            var asm = multiplexerType.Assembly;
            var messageType = asm.GetType("StackExchange.Redis.Message");
            var processorType = asm.GetType("StackExchange.Redis.ResultProcessor`1").MakeGenericType(genericType);
            var stateType = typeof(object);
            var serverType = asm.GetType("StackExchange.Redis.ServerEndPoint");

            var originalMethod = DynamicMethodBuilder<Func<object, object, object, object, object, Task<T>>>
               .CreateMethodCallDelegate(
                    multiplexerType,
                    "ExecuteAsyncImpl",
                    new[] { messageType, processorType, stateType, serverType },
                    new[] { genericType });

            using (var scope = CreateScope(multiplexer, message, server, finishOnClose: false))
            {
                return scope.Span.Trace(() => originalMethod(multiplexer, message, processor, state, server));
            }
        }

        private static Scope CreateScope(object multiplexer, object message, object server, bool finishOnClose = true)
        {
            var config = GetConfiguration(multiplexer);
            var hostAndPort = GetHostAndPort(config);
            var rawCommand = GetRawCommand(multiplexer, message);

            return Integrations.Redis.CreateScope(hostAndPort.Item1, hostAndPort.Item2, rawCommand);
        }
    }
}
