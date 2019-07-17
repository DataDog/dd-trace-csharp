using System;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;

namespace Datadog.Trace.ClrProfiler.Integrations.StackExchange.Redis
{
    /// <summary>
    /// Wraps calls to the StackExchange redis library.
    /// </summary>
    public static class ConnectionMultiplexer
    {
        private const string IntegrationName = "StackExchangeRedis";
        private const string RedisAssembly = "StackExchange.Redis";
        private const string StrongNameRedisAssembly = "StackExchange.Redis.StrongName";
        private const string ConnectionMultiplexerTypeName = "StackExchange.Redis.ConnectionMultiplexer";
        private const string Major1 = "1";
        private const string Major2 = "2";

        /// <summary>
        /// Execute a synchronous redis operation.
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="multiplexer">The connection multiplexer running the command.</param>
        /// <param name="message">The message to send to redis.</param>
        /// <param name="processor">The processor to handle the result.</param>
        /// <param name="server">The server to call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>The result</returns>
        [InterceptMethod(
            Integration = IntegrationName,
            CallerAssembly = RedisAssembly,
            TargetAssembly = RedisAssembly,
            TargetType = ConnectionMultiplexerTypeName,
            TargetSignatureTypes = new[] { "T", "StackExchange.Redis.Message", "StackExchange.Redis.ResultProcessor`1<T>", "StackExchange.Redis.ServerEndPoint" },
            TargetMinimumVersion = Major1,
            TargetMaximumVersion = Major2)]
        [InterceptMethod(
            Integration = IntegrationName,
            CallerAssembly = StrongNameRedisAssembly,
            TargetAssembly = StrongNameRedisAssembly,
            TargetType = ConnectionMultiplexerTypeName,
            TargetSignatureTypes = new[] { "T", "StackExchange.Redis.Message", "StackExchange.Redis.ResultProcessor`1<T>", "StackExchange.Redis.ServerEndPoint" },
            TargetMinimumVersion = Major1,
            TargetMaximumVersion = Major2)]
        public static T ExecuteSyncImpl<T>(
            object multiplexer,
            object message,
            object processor,
            object server,
            int opCode,
            int mdToken)
        {
            var resultType = typeof(T);
            var multiplexerType = multiplexer.GetType();
            var asm = multiplexerType.Assembly;
            var messageType = asm.GetType("StackExchange.Redis.Message");
            var processorType = asm.GetType("StackExchange.Redis.ResultProcessor`1").MakeGenericType(resultType);
            var serverType = asm.GetType("StackExchange.Redis.ServerEndPoint");

            var originalMethod = Emit.DynamicMethodBuilder<Func<object, object, object, object, T>>
               .CreateMethodCallDelegate(
                    multiplexerType,
                    methodName: "ExecuteSyncImpl",
                    (OpCodeValue)opCode,
                    methodParameterTypes: new[] { messageType, processorType, serverType },
                    methodGenericArguments: new[] { resultType });

            using (var scope = CreateScope(multiplexer, message))
            {
                try
                {
                    return originalMethod(multiplexer, message, processor, server);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
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
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>An asynchronous task.</returns>
        [InterceptMethod(
            Integration = IntegrationName,
            CallerAssembly = RedisAssembly,
            TargetAssembly = RedisAssembly,
            TargetType = ConnectionMultiplexerTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<T>", "StackExchange.Redis.Message", "StackExchange.Redis.ResultProcessor`1<T>", "System.Object", "StackExchange.Redis.ServerEndPoint" },
            TargetMinimumVersion = Major1,
            TargetMaximumVersion = Major2)]
        [InterceptMethod(
            Integration = IntegrationName,
            CallerAssembly = StrongNameRedisAssembly,
            TargetAssembly = StrongNameRedisAssembly,
            TargetType = ConnectionMultiplexerTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<T>", "StackExchange.Redis.Message", "StackExchange.Redis.ResultProcessor`1<T>", "System.Object", "StackExchange.Redis.ServerEndPoint" },
            TargetMinimumVersion = Major1,
            TargetMaximumVersion = Major2)]
        public static object ExecuteAsyncImpl<T>(
            object multiplexer,
            object message,
            object processor,
            object state,
            object server,
            int opCode,
            int mdToken)
        {
            var callOpCode = (OpCodeValue)opCode;
            return ExecuteAsyncImplInternal<T>(multiplexer, message, processor, state, server, callOpCode);
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
        /// <param name="callOpCode">The <see cref="OpCodeValue"/> used in the original method call.</param>
        /// <returns>An asynchronous task.</returns>
        private static async Task<T> ExecuteAsyncImplInternal<T>(object multiplexer, object message, object processor, object state, object server, OpCodeValue callOpCode)
        {
            var genericType = typeof(T);
            var multiplexerType = multiplexer.GetType();
            var asm = multiplexerType.Assembly;
            var messageType = asm.GetType("StackExchange.Redis.Message");
            var processorType = asm.GetType("StackExchange.Redis.ResultProcessor`1").MakeGenericType(genericType);
            var stateType = typeof(object);
            var serverType = asm.GetType("StackExchange.Redis.ServerEndPoint");

            var originalMethod = Emit.DynamicMethodBuilder<Func<object, object, object, object, object, Task<T>>>
                                     .CreateMethodCallDelegate(
                                          multiplexerType,
                                          methodName: "ExecuteAsyncImpl",
                                          callOpCode,
                                          methodParameterTypes: new[] { messageType, processorType, stateType, serverType },
                                          methodGenericArguments: new[] { genericType });

            using (var scope = CreateScope(multiplexer, message))
            {
                try
                {
                    return await originalMethod(multiplexer, message, processor, state, server).ConfigureAwait(false);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
            }
        }

        private static Scope CreateScope(object multiplexer, object message)
        {
            var config = StackExchangeRedisHelper.GetConfiguration(multiplexer);
            var hostAndPort = StackExchangeRedisHelper.GetHostAndPort(config);
            var rawCommand = StackExchangeRedisHelper.GetRawCommand(multiplexer, message);

            return RedisHelper.CreateScope(Tracer.Instance, IntegrationName, hostAndPort.Item1, hostAndPort.Item2, rawCommand);
        }
    }
}
