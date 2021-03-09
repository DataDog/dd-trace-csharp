using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WeatherService.Abstractions;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly IWeatherService WeatherServiceNetCore31 = ServiceProxy.Create<IWeatherService>(new Uri("fabric:/ServiceFabricApplication/WeatherService.NetCore31"));

        private static readonly IWeatherService WeatherServiceNetFx461 = ServiceProxy.Create<IWeatherService>(new Uri("fabric:/ServiceFabricApplication/WeatherService.NetFx461"));

        [HttpGet("test")]
        public string Test()
        {
            return "Hello, world!";
        }

        [HttpGet]
        public async Task<WeatherForecast> GetNetCore31()
        {
            return await WeatherServiceNetCore31.GetWeather("Hello, world!");
        }

        [HttpGet]
        public async Task<WeatherForecast> GetNetFx461()
        {
            return await WeatherServiceNetFx461.GetWeather("Hello, world!");
        }
    }
}
