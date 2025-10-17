using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
            var result = await _owmService.GetForecastAsync("Floreffe");
            if (result == null)
                return StatusCode(500, "OpenWeather service returned null");

            return Ok(result);
        }
        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var fakeWeather = new WeatherForecastDTO
            {
                DateWeather = DateTime.Now,
                TemperatureC = 18,
                Summary = "Sunny test",
                Humidity = 70,
                RainfallMm = 0,
                WindSpeedKmh = 10
            };

            await _weatherRepository.SaveWeatherForecastAsync(fakeWeather.MapToWeatherForecast());

            return Ok("Seed inserted");
        }
    }
}








































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.