using CitizeHackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Infrastructure.Services;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CityzenHackathon2025.API.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Numerics;

namespace Citizenhackathon2025.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly IOpenWeatherService _owmService;
        private readonly IMediator _mediator;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IWeatherForecastRepository weatherRepository, IHubContext<WeatherForecastHub> hubContext, IOpenWeatherService owmService, IMediator mediator, ILogger<WeatherForecastController> logger)
        {
            _weatherRepository = weatherRepository;
            _hubContext = hubContext;
            _owmService = owmService;
            _mediator = mediator;
            _logger = logger;
        }



        // Example of a Mediator call to retrieve historical forecasts
        [HttpGet("history")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetHistory([FromQuery] int limit = 10)
        {
            var history = await _mediator.Send(new GetWeatherHistoryQuery(limit));
            if (history == null || !history.Any())
                return NotFound("No weather history found.");

            return Ok(history);
        }

        // Get current weather via Mediator
        [HttpGet("current")]
        public async Task<ActionResult<WeatherForecastDTO>> GetCurrentWeather()
        {
            var currentForecast = await _mediator.Send(new GetCurrentWeatherQuery());

            if (currentForecast == null)
                return NotFound("Current weather data not found.");

            // SignalR notification
            await _hubContext.Clients.All.SendAsync("ReceiveWeather", currentForecast);

            return Ok(currentForecast);
        }

        // Retrieve weather by city, via external service OpenWeatherMap
        [HttpGet("weather/{city}")]
        public async Task<ActionResult<WeatherForecastDTO>> GetWeatherByCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City name must be provided.");

            var weatherDto = await _owmService.GetForecastAsync(city);

            if (weatherDto == null)
                return NotFound($"Unable to fetch weather data for city '{city}'.");

            await _hubContext.Clients.All.SendAsync("ExternalWeatherUpdate", weatherDto);

            return Ok(weatherDto);
        }
    }
}

