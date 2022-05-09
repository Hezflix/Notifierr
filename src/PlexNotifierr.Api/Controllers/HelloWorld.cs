using Microsoft.AspNetCore.Mvc;

namespace PlexNotifierr.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorld : ControllerBase
    {
        private readonly ILogger<HelloWorld> _logger;

        public HelloWorld(ILogger<HelloWorld> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public string Get()
        {
            return "Hello World";
        }
    }
}