using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Samples.AzureFunctions
{
    public static class WindowsFunctionExample
    {
        private static readonly HttpClient HttpClient;
        private const string IntervalInSeconds = "*/5 * * * * *";

        static WindowsFunctionExample()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        }

        public static IEnumerable<KeyValuePair<string, string>> EnvironmentSetup()
        {
            var prefixes = new[] { "COR_", "CORECLR_", "DD_", "DATADOG_" };

            return from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);
        }

        public static string[] GetUsefulStack()
        {
            var stackTrace = Environment.StackTrace;
            string[] methods = stackTrace.Split(new[] { " at " }, StringSplitOptions.None);
            return methods;
        }

        [FunctionName("TimerTrigger")]
        public static async Task Run([TimerTrigger(IntervalInSeconds)] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var stackTrace = GetUsefulStack();
            log.LogInformation($"Stack trace: {stackTrace}");
            LogEnvironmentVariables(log);
            await WriteJokeToConsole();
        }

        [FunctionName("HttpTrigger")]
        public static async Task<IActionResult> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            await WriteJokeToConsole();

            return new OkObjectResult(responseMessage);
        }

        private static async Task WriteJokeToConsole()
        {
            var joke = await HttpClient.GetStringAsync("https://icanhazdadjoke.com/");
            Console.WriteLine(joke);
        }

        private static void LogEnvironmentVariables(ILogger log)
        {
            foreach (var kvp in EnvironmentSetup())
            {
                log.LogInformation($"[ENV] {kvp.Key}: {kvp.Value}");
            }
        }

    }
}
