using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TempController : ControllerBase
    {
        private readonly IOpenWeatherService _owmService;
        private readonly IWeatherForecastRepository _weatherRepository;

        public TempController(IOpenWeatherService owmService, IWeatherForecastRepository weatherRepository)
        {
            _owmService = owmService;
            _weatherRepository = weatherRepository;
        }

        [HttpGet("test-weather")]
        public async Task<IActionResult> TestWeather()
        {
            var result = await _owmService.GetCurrentWeatherAsync("Brussels,BE");

            if (result == null)
                return StatusCode(503, "OpenWeather service returned null");

            return Ok(result);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed(CancellationToken ct)
        {
            var fakeWeather = new WeatherForecastDTO
            {
                DateWeather = DateTime.UtcNow,
                TemperatureC = 18,
                Summary = "Sunny test",
                Humidity = 70,
                RainfallMm = 0,
                WindSpeedKmh = 10
            };

            await _weatherRepository.SaveOrUpdateAsync(fakeWeather.MapToWeatherForecast(), ct);

            return Ok("Seed inserted");
        }

        //=============================================================
        //Debug Tools for OpenWeather integration (uncomment if needed)
        //=============================================================


        //[HttpGet("test-weather-impl")]
        //public IActionResult TestWeatherImpl()
        //{
        //    return Ok(new
        //    {
        //        implementation = _owmService.GetType().FullName,
        //        assembly = _owmService.GetType().Assembly.FullName
        //    });
        //}

        //[HttpGet("test-weather-service")]
        //public async Task<IActionResult> TestWeatherService()
        //{
        //    var city = "Brussels,BE";
        //    var result = await _owmService.GetCurrentWeatherAsync(city);

        //    if (result == null)
        //        return StatusCode(503, new { city, error = "OpenWeather service returned null" });

        //    return Ok(new { city, result });
        //}

        //[HttpGet("test-weather-throw")]
        //public async Task<IActionResult> TestWeatherThrow()
        //{
        //    var result = await _owmService.GetCurrentWeatherAsync("Brussels,BE");
        //    return Ok(result);
        //}

        //[HttpGet("test-weather-debug")]
        //public async Task<IActionResult> TestWeatherDebug(
        //    [FromServices] IHttpClientFactory httpFactory,
        //    [FromServices] IOptions<OpenWeatherOptions> opt)
        //{
        //    var cfg = opt.Value;

        //    if (string.IsNullOrWhiteSpace(cfg.ApiKey))
        //    {
        //        return StatusCode(500, new
        //        {
        //            error = "OpenWeather ApiKey is missing",
        //            baseUrl = cfg.BaseUrl
        //        });
        //    }

        //    var client = httpFactory.CreateClient("OpenWeatherRaw");
        //    var url = $"data/2.5/weather?q=Brussels,BE&appid={cfg.ApiKey}&units=metric&lang=fr";

        //    using var resp = await client.GetAsync(url);
        //    var body = await resp.Content.ReadAsStringAsync();

        //    return Ok(new
        //    {
        //        status = (int)resp.StatusCode,
        //        reason = resp.ReasonPhrase,
        //        baseUrl = cfg.BaseUrl,
        //        apiKeyLoaded = true,
        //        body
        //    });
        //}
    }
}








































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.