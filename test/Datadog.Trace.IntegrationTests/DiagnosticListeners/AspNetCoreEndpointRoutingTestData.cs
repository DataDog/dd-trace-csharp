﻿#if !NETFRAMEWORK
using Datadog.Trace.TestHelpers;
using Xunit;
using static Datadog.Trace.IntegrationTests.DiagnosticListeners.ErrorHandlingHelper;

namespace Datadog.Trace.IntegrationTests.DiagnosticListeners
{
    public static class AspNetCoreEndpointRoutingTestData
    {
        private const string IndexEndpointName = "Datadog.Trace.Tests.DiagnosticListeners.HomeController.Index (Datadog.Trace.Tests)";
        private const string ErrorEndpointName = "Datadog.Trace.Tests.DiagnosticListeners.HomeController.Error (Datadog.Trace.Tests)";
        private const string MyTestEndpointName = "Datadog.Trace.Tests.DiagnosticListeners.MyTestController.Index (Datadog.Trace.Tests)";
        // private const string StatusCodeEndpointName = "Datadog.Trace.Tests.DiagnosticListeners.MyTestController.SetStatusCode (Datadog.Trace.Tests)";

        /// <summary>
        /// Gets data for Endpoint Routing tests with the feature flags disabled
        /// (URL, isError, Resource, Tags)
        /// </summary>
        public static TheoryData<string, int, bool, string, SerializableDictionary> WithoutFeatureFlag => new()
        {
            { "/", 200, false, "GET Home/Index", EmptyTags() },
            { "/Home", 200, false, "GET Home/Index", EmptyTags() },
            { "/Home/Index", 200, false, "GET Home/Index", EmptyTags() },
            { "/MyTest", 200, false, "GET MyTest/Index", EmptyTags() },
            { "/MyTest/index", 200, false, "GET MyTest/Index", EmptyTags() },
            { "/Api/index", 200, false, "GET api/Index", EmptyTags() },
            { "/Api/Value/3", 200, false, "GET api/Value/{value}", EmptyTags() },
            { "/Api/Value/100", 400, false, "GET api/Value/{value}", EmptyTags() },
            { "/statuscode", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/100", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/Oops", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/200", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/healthz", 200, false, "GET /healthz", EmptyTags() },
            { "/echo", 200, false, "GET /echo", EmptyTags() },
            { "/echo/123", 200, false, "GET /echo/?", EmptyTags() },
            { "/echo/false", 404, false, "GET /echo/false", EmptyTags() },
            { "/I/dont/123/exist/", 404, false, "GET /i/dont/?/exist/", EmptyTags() },
            { "/Home/Error", 500, true, "GET Home/Error", EmptyTags() },
            { "/Home/UncaughtError", 500, true, "GET Home/UncaughtError", EmptyTags() },
            { "/Home/BadHttpRequest", 400, true, "GET Home/BadHttpRequest", EmptyTags() },
            { $"{CustomHandlerPrefix}/Home/Error", 500, true, "GET Home/Error", EmptyTags() },
            { $"{CustomHandlerPrefix}/Home/UncaughtError", 500, true, "GET Home/UncaughtError", EmptyTags() },
            { $"{CustomHandlerPrefix}/Home/BadHttpRequest", 500, true, "GET Home/BadHttpRequest", EmptyTags() },
            { $"{CustomHandlerPrefix}/throws", 500, true, $"GET {CustomHandlerPrefix}/throws", EmptyTags() },
            { $"{ExceptionPagePrefix}/Home/Error", 500, true, "GET Home/Error", EmptyTags() },
            { $"{ExceptionPagePrefix}/Home/BadHttpRequest", 400, true, "GET Home/BadHttpRequest", EmptyTags() },
            { $"{ExceptionPagePrefix}/throws", 500, true, $"GET {ExceptionPagePrefix}/throws", EmptyTags() },
            // { $"{ReExecuteHandlerPrefix}/Home/Error", 500, true, "GET Home/Error", EmptyTags() },
            // { $"{ReExecuteHandlerPrefix}/Home/BadHttpRequest", 500, true, "GET Home/BadHttpRequest", EmptyTags() },
            // { $"{ReExecuteHandlerPrefix}/throws", 500, true, $"GET {ReExecuteHandlerPrefix}/throws", EmptyTags() },
        };

        /// <summary>
        /// Gets data for Endpoint Routing tests with the feature flags enabled
        /// (URL, isError, Resource, Tags)
        /// </summary>
        public static TheoryData<string, int, bool, string, SerializableDictionary> WithFeatureFlag => new()
        {
            { "/", 200, false, "GET /home/index", ConventionalRouteTags(endpoint: IndexEndpointName) },
            { "/Home", 200, false, "GET /home/index", ConventionalRouteTags(endpoint: IndexEndpointName) },
            { "/Home/Index", 200, false, "GET /home/index", ConventionalRouteTags(endpoint: IndexEndpointName) },
            { "/Api/index", 200, false, "GET /api/index", ApiIndexTags() },
            { "/Api/Value/3", 200, false, "GET /api/value/{value}", ApiValueTags() },
            { "/Api/Value/100", 400, false, "GET /api/value/{value}", ApiValueTags() },
            { "/MyTest", 200, false, "GET /mytest/index", ConventionalRouteTags(controller: "mytest", endpoint: MyTestEndpointName) },
            { "/MyTest/index", 200, false, "GET /mytest/index", ConventionalRouteTags(controller: "mytest", endpoint: MyTestEndpointName) },
            { "/statuscode", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/100", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/Oops", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/200", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/healthz", 200, false, "GET /healthz", HealthCheckTags() },
            { "/echo", 200, false, "GET /echo", EchoTags() },
            { "/echo/123", 200, false, "GET /echo/{value?}", EchoTags() },
            { "/echo/false", 404, false, "GET /echo/false", EmptyTags() },
            { "/I/dont/123/exist/", 404, false, "GET /i/dont/?/exist/", EmptyTags() },
            { "/Home/Error", 500, true, "GET /home/error", ConventionalRouteTags(action: "error") },
            { "/Home/UncaughtError", 500, true, "GET /home/uncaughterror", ConventionalRouteTags(action: "uncaughterror") },
            { "/Home/BadHttpRequest", 400, true, "GET /home/badhttprequest", ConventionalRouteTags(action: "badhttprequest") },
            { $"{CustomHandlerPrefix}/Home/Error", 500, true, $"GET {CustomHandlerPrefix}/home/Error", ConventionalRouteTags(action: "error") },
            { $"{CustomHandlerPrefix}/Home/UncaughtError", 500, true, $"GET {CustomHandlerPrefix}/home/uncaughterror", ConventionalRouteTags(action: "uncaughterror") },
            { $"{CustomHandlerPrefix}/Home/BadHttpRequest", 500, true, $"GET {CustomHandlerPrefix}/home/badhttprequest", ConventionalRouteTags(action: "badhttprequest") },
            { $"{ExceptionPagePrefix}/Home/Error", 500, true, $"GET {ExceptionPagePrefix}/home/Error", ConventionalRouteTags(action: "error") },
            { $"{ExceptionPagePrefix}/Home/BadHttpRequest", 400, true, $"GET {ExceptionPagePrefix}/home/badhttprequest", ConventionalRouteTags(action: "badhttprequest") },
            { $"{ReExecuteHandlerPrefix}/Home/Error", 500, true, $"GET {ReExecuteHandlerPrefix}/home/Error", ConventionalRouteTags(action: "error") },
            { $"{ReExecuteHandlerPrefix}/Home/BadHttpRequest", 500, true, $"GET {ReExecuteHandlerPrefix}/home/badhttprequest", ConventionalRouteTags(action: "badhttprequest") },
        };

        private static SerializableDictionary EmptyTags() => new()
        {
            { Tags.AspNetRoute, null },
            { Tags.AspNetController, null },
            { Tags.AspNetAction, null },
            // { Tags.AspNetEndpoint, endpoint },
        };

        private static SerializableDictionary ConventionalRouteTags(
            string action = "index",
            string controller = "home",
            string endpoint = null) => new()
        {
            { Tags.AspNetRoute, "{controller=home}/{action=index}/{id?}" },
            { Tags.AspNetController, controller },
            { Tags.AspNetAction, action },
            // { Tags.AspNetEndpoint, endpoint },
        };

        private static SerializableDictionary StatusCodeTags() => new()
        {
            { Tags.AspNetRoute, "statuscode/{value=200}" },
            { Tags.AspNetController, "mytest" },
            { Tags.AspNetAction, "setstatuscode" },
            // { Tags.AspNetEndpoint, StatusCodeEndpointName },
        };

        private static SerializableDictionary HealthCheckTags() => new()
        {
            { Tags.AspNetRoute, "/healthz" },
            // { Tags.AspNetEndpoint, "Custom Health Check" },
        };

        private static SerializableDictionary EchoTags() => new()
        {
            { Tags.AspNetRoute, "/echo/{value:int?}" },
            // { Tags.AspNetEndpoint, "/echo/{value:int?} HTTP: GET" },
        };

        private static SerializableDictionary ApiIndexTags() => new()
        {
            { Tags.AspNetRoute, "api/index" },
            { Tags.AspNetController, "api" },
            { Tags.AspNetAction, "index" },
            // { Tags.AspNetEndpoint, endpoint },
        };

        private static SerializableDictionary ApiValueTags() => new()
        {
            { Tags.AspNetRoute, "api/value/{value}" },
            { Tags.AspNetController, "api" },
            { Tags.AspNetAction, "value" },
            // { Tags.AspNetEndpoint, endpoint },
        };
    }
}
#endif
