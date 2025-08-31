using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Repositories.Providers.Hubs;
using CitizenHackathon2025.Application.CQRS.Queries;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.WeatherForecasts.Queries;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        // current via OWM (existant)
        [HttpGet("current")]
        public async Task<ActionResult<WeatherForecastDTO>> GetCurrent([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City parameter is required.");

            var current = await _mediator.Send(new GetCurrentWeatherQuery(city));
            if (current == null) return NotFound();

            await _hubContext.Clients.All.SendAsync("ReceiveForecast", current);
            return Ok(current);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetHistory([FromQuery] int limit = 10)
        {
            var history = await _mediator.Send(new GetWeatherHistoryQuery(limit));
            return history == null || !history.Any() ? NotFound() : Ok(history);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WeatherForecastDTO>> GetById(int id)
        {
            var dto = await _mediator.Send(new GetWeatherForecastByIdQuery(id));
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetAll()
        {
            var list = await _mediator.Send(new GetAllWeatherForecastsQuery());
            return list == null || !list.Any() ? NotFound() : Ok(list);
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.