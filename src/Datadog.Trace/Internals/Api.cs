﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Datadog.Trace.Logging;
using MsgPack.Serialization;

namespace Datadog.Trace
{
    internal class Api : IApi
    {
        private const string TracesPath = "/v0.3/traces";
        private const string ServicesPath = "/v0.3/services";

#if !NETSTANDARD2_0
        private static readonly string _frameworkDescription = string.Format(".NET Framework {0}", typeof(object).GetTypeInfo().Assembly.GetName().Version);
#endif

        private static ILog _log = LogProvider.For<Api>();
        private static SerializationContext _serializationContext;

        private Uri _tracesEndpoint;
        private Uri _servicesEndpoint;
        private HttpClient _client;

        static Api()
        {
            _serializationContext = new SerializationContext();
            var spanSerializer = new SpanMessagePackSerializer(_serializationContext);
            var serviceSerializer = new ServiceInfoMessagePackSerializer(_serializationContext);
            _serializationContext.ResolveSerializer += (sender, eventArgs) =>
            {
                if (eventArgs.TargetType == typeof(Span))
                {
                    eventArgs.SetSerializer(spanSerializer);
                }

                if (eventArgs.TargetType == typeof(ServiceInfo))
                {
                    eventArgs.SetSerializer(serviceSerializer);
                }
            };
        }

        public Api(Uri baseEndpoint, DelegatingHandler delegatingHandler = null)
        {
            if (delegatingHandler != null)
            {
                _client = new HttpClient(delegatingHandler);
            }
            else
            {
                _client = new HttpClient();
            }

            _tracesEndpoint = new Uri(baseEndpoint, TracesPath);
            _servicesEndpoint = new Uri(baseEndpoint, ServicesPath);

            // TODO:bertrand add header for os version
            _client.DefaultRequestHeaders.Add("Datadog-Meta-Lang", ".NET");
#if NETSTANDARD2_0
            _client.DefaultRequestHeaders.Add("Datadog-Meta-Lang-Interpreter", RuntimeInformation.FrameworkDescription);
#else
            _client.DefaultRequestHeaders.Add("Datadog-Meta-Lang-Interpreter", _frameworkDescription);
#endif
            _client.DefaultRequestHeaders.Add("Datadog-Meta-Tracer-Version", Assembly.GetAssembly(typeof(Api)).GetName().Version.ToString());
        }

        public async Task SendTracesAsync(IList<List<Span>> traces)
        {
            await SendAsync(traces, _tracesEndpoint);
        }

        public async Task SendServiceAsync(ServiceInfo service)
        {
            await SendAsync(service, _servicesEndpoint);
        }

        private async Task SendAsync<T>(T value, Uri endpoint)
        {
            try
            {
                var content = new MsgPackContent<T>(value, _serializationContext);
                var response = await _client.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _log.ErrorException("An error occured while sending traces to the agent at {Endpoint}", ex, endpoint);
            }
        }
    }
}
