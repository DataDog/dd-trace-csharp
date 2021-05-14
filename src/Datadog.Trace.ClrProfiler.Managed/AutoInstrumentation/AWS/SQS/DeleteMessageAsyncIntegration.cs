using System;
using System.Threading;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// AWSSDK.SQS DeleteMessageAsync calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "AWSSDK.SQS",
        TypeName = "Amazon.SQS.AmazonSQSClient",
        MethodName = "DeleteMessageAsync",
        ReturnTypeName = "System.Threading.Tasks.Task`1<Amazon.SQS.Model.DeleteMessageResponse>",
        ParameterTypeNames = new[] { "Amazon.SQS.Model.DeleteMessageRequest", ClrNames.CancellationToken },
        MinimumVersion = "3.0.0",
        MaximumVersion = "3.*.*",
        IntegrationName = AwsConstants.IntegrationName)]
    public class DeleteMessageAsyncIntegration
    {
        private const string Operation = "DeleteMessage";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TDeleteMessageRequest">Type of the request object</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method</param>
        /// <param name="request">The request for the SQS operation</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TDeleteMessageRequest>(TTarget instance, TDeleteMessageRequest request, CancellationToken cancellationToken)
            where TDeleteMessageRequest : IAmazonSQSRequestWithQueueUrl, IDuckType
        {
            if (request.Instance is null)
            {
                return CallTargetState.GetDefault();
            }

            var scope = AwsSqsCommon.CreateScope(Tracer.Instance, $"{AwsConstants.AwsService}.{Operation}", out AwsSqsTags tags);
            tags.Operation = Operation;
            tags.Service = AwsConstants.AwsService;
            tags.QueueUrl = request.QueueUrl;

            return new CallTargetState(scope);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">Type of the response, in an async scenario will be T of Task of T</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="response">Response instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static TResponse OnAsyncMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, CallTargetState state)
            where TResponse : IAmazonWebServiceResponse
        {
            if (state.Scope?.Span.Tags is AwsSqsTags tags)
            {
                tags.RequestId = response.ResponseMetadata.RequestId;

                state.Scope.Span.SetHttpStatusCode((int)response.HttpStatusCode, isServer: false);
            }

            state.Scope.DisposeWithException(exception);
            return response;
        }
    }
}
