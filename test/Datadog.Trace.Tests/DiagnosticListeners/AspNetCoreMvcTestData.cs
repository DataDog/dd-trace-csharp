﻿using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.Tests.DiagnosticListeners
{
    public static class AspNetCoreMvcTestData
    {
        /// <summary>
        /// Gets data for MVC tests with the feature flags disabled
        /// (URL, isError, Resource, Tags)
        /// </summary>
        public static TheoryData<string, int, bool, string, SerializableDictionary> WithoutFeatureFlag => new()
        {
#if NETCOREAPP2_1
            { "/", 200, false, "GET Home/Index", EmptyTags() },
#else
            { "/", 200, false, "GET ", EmptyTags() },
#endif
            { "/Home", 200, false, "GET Home/Index", EmptyTags() },
            { "/Home/Index", 200, false, "GET Home/Index", EmptyTags() },
            { "/Home/Error", 500, true, "GET Home/Error", EmptyTags() },
            { "/MyTest", 200, false, "GET MyTest/Index", EmptyTags() },
            { "/MyTest/index", 200, false, "GET MyTest/Index", EmptyTags() },
            { "/statuscode", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/100", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/Oops", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/statuscode/200", 200, false, "GET statuscode/{value=200}", EmptyTags() },
            { "/I/dont/123/exist/", 404, false, "GET /i/dont/?/exist/", EmptyTags() },
        };

        /// <summary>
        /// Gets data for MVC tests with the feature flags enabled
        /// (URL, isError, Resource, Tags)
        /// </summary>
        public static TheoryData<string, int, bool, string, SerializableDictionary> WithFeatureFlag => new()
        {
            { "/", 200, false, "GET /home/index", ConventionalRouteTags() },
            { "/Home", 200, false, "GET /home/index", ConventionalRouteTags() },
            { "/Home/Index", 200, false, "GET /home/index", ConventionalRouteTags() },
            { "/Home/Error", 500, true, "GET /home/error", ConventionalRouteTags(action: "error") },
            { "/MyTest", 200, false, "GET /mytest/index", ConventionalRouteTags(controller: "mytest") },
            { "/MyTest/index", 200, false, "GET /mytest/index", ConventionalRouteTags(controller: "mytest") },
            { "/statuscode", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/100", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/Oops", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/statuscode/200", 200, false, "GET /statuscode/{value}", StatusCodeTags() },
            { "/I/dont/123/exist/", 404, false, "GET /i/dont/?/exist/", EmptyTags() },
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
            string controller = "home") => new()
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
            // { Tags.AspNetEndpoint, endpoint },
        };
    }
}
