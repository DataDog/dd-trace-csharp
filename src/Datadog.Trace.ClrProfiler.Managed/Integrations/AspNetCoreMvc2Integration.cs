using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// The ASP.NET Core MVC 2 integration.
    /// </summary>
    public static class AspNetCoreMvc2Integration
    {
        internal const string OperationName = "aspnet-core-mvc.request";
        private const string HttpContextKey = "__Datadog.Trace.ClrProfiler.Integrations." + nameof(AspNetCoreMvc2Integration);
        private const string TypeName = "Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions";
        private const string AssemblyName = "Microsoft.AspNetCore.Mvc.Core";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(AspNetCoreMvc2Integration));

        private static readonly Type TargetType = Type.GetType(
            $"{TypeName}, {AssemblyName}",
            throwOnError: false,
            ignoreCase: false);

        /// <summary>
        /// Wrapper method used to instrument Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions.BeforeAction()
        /// </summary>
        /// <param name="diagnosticSource">The DiagnosticSource that this extension method was called on.</param>
        /// <param name="actionDescriptor">An ActionDescriptor with information about the current action.</param>
        /// <param name="httpContext">The HttpContext for the current request.</param>
        /// <param name="routeData">A RouteData with information about the current route.</param>
        [InterceptMethod(
            CallerAssembly = AssemblyName,
            TargetAssembly = AssemblyName,
            TargetType = TypeName)]
        public static void BeforeAction(
            object diagnosticSource,
            object actionDescriptor,
            object httpContext,
            object routeData)
        {
            // get delegate for target method
            var target = DynamicMethodBuilder<Action<object, object, object, object>>.GetOrCreateMethodCallDelegate(
                TargetType,
                nameof(BeforeAction));

            if (target == null)
            {
                // we cannot call target method, but profiled app should continue working
                var fullMethodName = $"{TypeName}.{nameof(BeforeAction)}()";
                Log.WarnFormat("Could not create delegate for {0}", fullMethodName);
                return;
            }

            // save the scope se we can access it in AfterAction()
            var scope = CreateScope(actionDescriptor, httpContext);
            SetHttpContextItem(httpContext, HttpContextKey, scope);

            try
            {
                // execute target method
                target(diagnosticSource, actionDescriptor, httpContext, routeData);
            }
            catch (Exception ex)
            {
                // don't add this exception to span, it was not thrown by controller action,
                // profiled app should continue working
                Log.ErrorException("Error executing target method.", ex);
            }
        }

        /// <summary>
        /// Wrapper method used to instrument Microsoft.AspNetCore.Mvc.Internal.MvcCoreDiagnosticSourceExtensions.AfterAction()
        /// </summary>
        /// <param name="diagnosticSource">The DiagnosticSource that this extension method was called on.</param>
        /// <param name="actionDescriptor">An ActionDescriptor with information about the current action.</param>
        /// <param name="httpContext">The HttpContext for the current request.</param>
        /// <param name="routeData">A RouteData with information about the current route.</param>
        [InterceptMethod(
            CallerAssembly = AssemblyName,
            TargetAssembly = AssemblyName,
            TargetType = TypeName)]
        public static void AfterAction(
            object diagnosticSource,
            object actionDescriptor,
            dynamic httpContext,
            object routeData)
        {
            var target = DynamicMethodBuilder<Action<object, object, object, object>>.GetOrCreateMethodCallDelegate(
                TargetType,
                nameof(AfterAction));

            if (target == null)
            {
                // we cannot call target method, but profiled app should continue working
                var fullMethodName = $"{TypeName}.{nameof(BeforeAction)}()";
                Log.WarnFormat("Could not create delegate for {0}", fullMethodName);
                return;
            }

            using (var scope = GetHttpContextItem(httpContext, HttpContextKey) as Scope)
            {
                try
                {
                    target.Invoke(diagnosticSource, actionDescriptor, httpContext, routeData);
                }
                catch (Exception ex)
                {
                    // don't add this exception to span, it was not thrown by controller action,
                    // profiled app should continue working
                    Log.ErrorException("Error executing target method.", ex);
                }

                // add tags that are only available after MVC action is executed
                scope?.Span?.SetTag("http.status_code", httpContext?.Response.StatusCode.ToString());
            }
        }

        private static Scope CreateScope(object actionDescriptor, object httpContext)
        {
            string controllerName = actionDescriptor.GetProperty<string>("ControllerName").GetValueOrDefault();
            string actionName = actionDescriptor.GetProperty<string>("ActionName").GetValueOrDefault();
            string routeTemplate = actionDescriptor.GetProperty("AttributeRouteInfo").GetProperty<string>("Template").GetValueOrDefault();

            var request = httpContext.GetProperty("Request");
            string httpMethod = request.GetProperty<string>("Method").ToString().ToUpperInvariant();
            string url = GetDisplayUrl(request).ToLowerInvariant();

            Scope scope = Tracer.Instance.StartActive(OperationName);
            Span span = scope.Span;

            span.Type = SpanTypes.Web;
            span.ResourceName = $"{httpMethod} {controllerName}.{actionName}";
            span.SetTag(Tags.HttpMethod, httpMethod);
            span.SetTag(Tags.HttpUrl, url);
            span.SetTag(Tags.AspNetRoute, routeTemplate);
            span.SetTag(Tags.AspNetController, controllerName);
            span.SetTag(Tags.AspNetAction, actionName);

            return scope;
        }

        private static void SetHttpContextItem(object httpContext, object key, object value)
        {
            if (httpContext.TryGetPropertyValue("Items", out IDictionary<object, object> items))
            {
                items[key] = value;
            }
        }

        private static object GetHttpContextItem(object httpContext, object key)
        {
            if (httpContext.TryGetPropertyValue("Items", out IDictionary<object, object> items))
            {
                return items[key];
            }

            return null;
        }

        private static string GetDisplayUrl(MemberResult<object> request)
        {
            string scheme = request.GetProperty<string>("Scheme").ToString();
            string host = request.GetProperty("Host").GetProperty<string>("Value").ToString();
            string pathBase = request.GetProperty("PathBase").GetProperty<string>("Value").ToString();
            string path = request.GetProperty("Path").GetProperty<string>("Value").ToString();
            string queryString = request.GetProperty("QueryString").GetProperty<string>("Value").ToString();

            return $"{scheme}://{host}{pathBase}{path}{queryString}";
        }
    }
}
