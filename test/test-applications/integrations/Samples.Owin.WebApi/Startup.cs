using System.Web.Http;
using Datadog.Trace.Owin;
using Owin;

// [assembly: OwinStartup(typeof(Startup))]
namespace Samples.Owin.WebApi
{
    public class Startup
    {
        public static void Configuration(IAppBuilder appBuilder)
        {
            // Insert .NET Tracer before any other middleware so the Datadog trace
            // will wrap the rest of the OWIN pipeline
            appBuilder.UseDatadogTracingOwinMiddleware();
            // appBuilder.Use<SomeOtherMiddleware>();

            var config = new HttpConfiguration();

            // Attribute routing.
            config.MapHttpAttributeRoutes();

            // Convention-based routing.
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);
        }
    }
}