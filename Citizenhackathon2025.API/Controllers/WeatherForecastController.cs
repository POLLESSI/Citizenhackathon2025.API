using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.LocalBusinessRules.Validations;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.Repositories.Providers.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly IHubContext<WeatherHub> _hubContext;
        private readonly IOpenWeatherService _owmService; // ton service OWM
        private readonly IMediator _mediator;

        public WeatherForecastController(
            IWeatherForecastRepository weatherRepository,
            IHubContext<WeatherHub> hubContext,
            IOpenWeatherService owmService,
            IMediator mediator)
        {
            _weatherRepository = weatherRepository;
            _hubContext = hubContext;
            _owmService = owmService;
            _mediator = mediator;
        }

        // Injection manuelle (test)
        [HttpPost("manual")]
        public async Task<ActionResult<WeatherForecastDTO>> PostManual([FromBody] WeatherForecastDTO dto)
        {
            if (dto == null) return BadRequest();

            // upsert + broadcast
            var entity = dto.MapToWeatherForecast();
            var saved = await _weatherRepository.SaveOrUpdateAsync(entity);
            var result = saved.MapToWeatherForecastDTO();

            await _hubContext.Clients.All.SendAsync("ReceiveForecast", result);
            return Ok(result);
        }

        // current via OWM (existing)
        [HttpGet("current")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WeatherForecastDTO))]
        public async Task<IActionResult> GetCurrent([FromQuery] string city, CancellationToken ct = default)
        {
            var isValid = !string.IsNullOrWhiteSpace(city) && WeatherForecastValidator.IsCityNameValid(city);

            if (!isValid)
            {
                // City invalide → 200 avec fallback explicite
                Response.Headers["X-Input-Valid"] = "false";
                return Ok(new WeatherForecastDTO
                {
                    DateWeather = DateTime.UtcNow,
                    TemperatureC = 0,
                    Summary = "Invalid city",
                    RainfallMm = 0.0,
                    Humidity = 0,
                    WindSpeedKmh = 0.0,
                    Icon = null,
                    IconUrl = "",
                    WeatherMain = "",
                    IsSevere = false,
                    Description = "Paramètre 'city' invalide. Exemple : 'Namur'."
                });
            }

            var current = await _mediator.Send(new GetCurrentWeatherQuery(city), ct);

            if (current is null)
            {
                // No data → 200 with fallback
                Response.Headers["X-Data-Source"] = "fallback";
                return Ok(new WeatherForecastDTO
                {
                    DateWeather = DateTime.UtcNow,
                    TemperatureC = 0,
                    Summary = "No data available",
                    RainfallMm = 0.0,
                    Humidity = 0,
                    WindSpeedKmh = 0.0,
                    Icon = null,
                    IconUrl = "",
                    WeatherMain = "",
                    IsSevere = false,
                    Description = $"No current weather forecast for '{city}'."
                });
            }

            // (Optional) SignalR side effect — comment out if you want a strictly idempotent GET
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", current, ct);

            return Ok(current);
        }

        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherForecastDTO>))]
        public async Task<IActionResult> GetHistory([FromQuery, Range(1, 500)] int limit = 10, CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 500);

            var history = await _mediator.Send(new GetWeatherHistoryQuery(limit), ct);

            // Always 200, even if empty
            return Ok(history ?? new List<WeatherForecastDTO>());
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WeatherForecastDTO))]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                Response.Headers["X-Input-Valid"] = "false";
                return Ok(new WeatherForecastDTO
                {
                    DateWeather = DateTime.UtcNow,
                    TemperatureC = 0,
                    Summary = "Invalid id",
                    RainfallMm = 0.0,
                    Humidity = 0,
                    WindSpeedKmh = 0.0,
                    IsSevere = false,
                    Description = "Id must be greater than zero."
                });
            }

            var dto = await _mediator.Send(new GetWeatherForecastByIdQuery(id), ct);

            if (dto is null)
            {
                Response.Headers["X-Resource-Found"] = "false";
                return Ok(new WeatherForecastDTO
                {
                    Id = id,
                    DateWeather = DateTime.UtcNow,
                    TemperatureC = 0,
                    Summary = "Not found",
                    RainfallMm = 0.0,
                    Humidity = 0,
                    WindSpeedKmh = 0.0,
                    IsSevere = false,
                    Description = $"Forecast with id={id} was not found."
                });
            }

            return Ok(dto);
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetAll()
        {
            var list = await _mediator.Send(new GetAllWeatherForecastsQuery());
            return Ok(list ?? new List<WeatherForecastDTO>()); // 200 []
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.