using System;
using System.IO;
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
        private static readonly HttpClient JokeHttpClient;
        private static readonly HttpClient FunctionHttpClient;
        private static string _httpFunctionUrl;

        private const string IntervalInSeconds = "*/60 * * * * *";

        static WindowsFunctionExample()
        {
            FunctionHttpClient = new HttpClient();
            JokeHttpClient = new HttpClient();
            JokeHttpClient.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        }

        // [FunctionName("SimpleTimer")]
        // public static async Task SimpleTimer([TimerTrigger(IntervalInSeconds)] TimerInfo myTimer, ILogger log)
        // {
        //     log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        //     await WriteJokeToConsole(log);
        // }

        [FunctionName("DistributedTimer")]
        public static async Task DistributedTimer([TimerTrigger(IntervalInSeconds)] TimerInfo myTimer, ILogger log)
        {
            _httpFunctionUrl = _httpFunctionUrl ?? Environment.GetEnvironmentVariable("DD_FUNCTION_HOST_BASE") ?? "localhost:7071";
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var url = $"http://{_httpFunctionUrl}";
            var simpleResponse = await FunctionHttpClient.GetStringAsync($"{url}/api/SimpleHttp");
            log.LogWarning(simpleResponse);
            var slowResponse = await FunctionHttpClient.GetStringAsync($"{url}/api/slow");
            log.LogWarning(slowResponse);
        }

        [FunctionName("SimpleHttp")]
        public static async Task<IActionResult> SimpleHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var joke = await WriteJokeToConsole(log);

            string responseMessage = string.IsNullOrEmpty(name)
                                         ? $"{joke}. Pass a name in the query string or in the request body for a personalized response."
                                         : $"Hello, {name}. {joke}.";

            return new OkObjectResult(responseMessage);
        }


        [FunctionName("SlowHttp")]
        public static async Task<IActionResult> SlowHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "slow")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await Task.Delay(100);

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var joke = await WriteJokeToConsole(log);

            string responseMessage = string.IsNullOrEmpty(name)
                                         ? $"{joke}. Pass a name in the query string or in the request body for a personalized response."
                                         : $"Hello, {name}. {joke}.";

            await Task.Delay(100);

            return new OkObjectResult(responseMessage);
        }

        private static async Task<string> WriteJokeToConsole(ILogger log)
        {
            var joke = await JokeHttpClient.GetStringAsync("https://icanhazdadjoke.com/");
            log.LogWarning(joke);
            return joke;
        }
    }
}
