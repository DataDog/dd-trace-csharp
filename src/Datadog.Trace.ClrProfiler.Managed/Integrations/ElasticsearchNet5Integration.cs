using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Traces an Elasticsearch pipeline
    /// </summary>
    public static class ElasticsearchNet5Integration
    {
        private const string IntegrationName = "ElasticsearchNet5";
        private const string Version5 = "5";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(ElasticsearchNet5Integration));
        private static readonly Type ElasticsearchResponseType = Type.GetType("Elasticsearch.Net.ElasticsearchResponse`1, Elasticsearch.Net", throwOnError: false);

        /// <summary>
        /// Traces a synchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            CallerAssembly = "Elasticsearch.Net",
            TargetAssembly = "Elasticsearch.Net",
            TargetType = "Elasticsearch.Net.IRequestPipeline",
            TargetSignatureTypes = new[] { "Elasticsearch.Net.ElasticsearchResponse`1<T>", "Elasticsearch.Net.RequestData" },
            TargetMinimumVersion = Version5,
            TargetMaximumVersion = Version5)]
        public static object CallElasticsearch<TResponse>(object pipeline, object requestData, int opCode, int mdToken)
        {
            // TResponse CallElasticsearch<TResponse>(RequestData requestData) where TResponse : class, IElasticsearchResponse, new();
            var originalMethod = Emit.DynamicMethodBuilder<Func<object, object, object>>
                                     .GetOrCreateMethodCallDelegate(
                                          ElasticsearchNetCommon.RequestPipelineType,
                                          "CallElasticsearch",
                                          methodGenericArguments: new[] { typeof(TResponse) });

            using (var scope = ElasticsearchNetCommon.CreateScope(Tracer.Instance, IntegrationName, pipeline, requestData))
            {
                try
                {
                    return originalMethod(pipeline, requestData);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
            }
        }

        /// <summary>
        /// Traces an asynchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="TResponse">Type type of the response</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationTokenSource">A cancellation token</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            CallerAssembly = "Elasticsearch.Net",
            TargetAssembly = "Elasticsearch.Net",
            TargetType = "Elasticsearch.Net.IRequestPipeline",
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<Elasticsearch.Net.ElasticsearchResponse`1<T>>", "Elasticsearch.Net.RequestData", ClrNames.CancellationToken },
            TargetMinimumVersion = Version5,
            TargetMaximumVersion = Version5)]
        public static object CallElasticsearchAsync<TResponse>(object pipeline, object requestData, object cancellationTokenSource, int opCode, int mdToken)
        {
            // Task<ElasticsearchResponse<TReturn>> CallElasticsearchAsync<TReturn>(RequestData requestData, CancellationToken cancellationToken) where TReturn : class;
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            var genericResponseType = ElasticsearchResponseType.MakeGenericType(typeof(TResponse));

            var genericArgument = typeof(TResponse);

            Func<object, object, CancellationToken, object> instrumentedMethod;

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, object, CancellationToken, object>>
                       .Start(Assembly.GetCallingAssembly(), mdToken, opCode)
                       .WithConcreteType(pipeline.GetType())
                       .WithMethodGenericArguments(genericArgument)
                       .WithParameters(requestData, cancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorException($"Error resolving Elasticsearch.Net.IRequestPipeline.{nameof(CallElasticsearchAsync)}<T>(...)", ex);
                throw;
            }

            return AsyncHelper.InvokeGenericTaskDelegate(
                owningType: ElasticsearchNetCommon.RequestPipelineType,
                taskResultType: genericResponseType,
                nameOfIntegrationMethod: nameof(CallElasticsearchAsyncInternal),
                integrationType: typeof(ElasticsearchNet5Integration),
                pipeline,
                requestData,
                cancellationToken,
                instrumentedMethod);
        }

        /// <summary>
        /// Traces an asynchronous call to Elasticsearch.
        /// </summary>
        /// <typeparam name="T">Type type of the Task</typeparam>
        /// <param name="pipeline">The pipeline for the original method</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <param name="originalMethod">A delegate for the method we are instrumenting</param>
        /// <returns>The original result</returns>
        private static async Task<T> CallElasticsearchAsyncInternal<T>(
            object pipeline,
            object requestData,
            CancellationToken cancellationToken,
            Func<object, object, CancellationToken, object> originalMethod)
        {
            using (var scope = ElasticsearchNetCommon.CreateScope(Tracer.Instance, IntegrationName, pipeline, requestData))
            {
                try
                {
                    var task = (Task<T>)originalMethod(pipeline, requestData, cancellationToken);
                    return await task.ConfigureAwait(false);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
            }
        }
    }
}
