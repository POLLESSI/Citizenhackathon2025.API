using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using System.ComponentModel.DataAnnotations;
using MediatR;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using HubEvents = CitizenHackathon2025.Shared.StaticConfig.Constants.WeatherForecastHubMethods;
namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly IOpenWeatherService _owmService; 
        private readonly IMediator _mediator;

        public WeatherForecastController(
            IWeatherForecastRepository weatherRepository,
            IHubContext<WeatherForecastHub> hubContext,
            IOpenWeatherService owmService,
            IMediator mediator)
        {
            _weatherRepository = weatherRepository;
            _hubContext = hubContext;
            _owmService = owmService;
            _mediator = mediator;
        }

        // standard create + broadcast
        [HttpPost]
        public async Task<ActionResult<WeatherForecastDTO>> Create([FromBody] WeatherForecastDTO dto)
        {
            if (dto is null) return BadRequest();

            var saved = await _weatherRepository.SaveOrUpdateAsync(dto.MapToWeatherForecast());
            var result = saved.MapToWeatherForecastDTO();

            await _hubContext.Clients.All.SendAsync("ReceiveForecast", result); // broadcast
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // Manual injection (test)
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

        // generate + broadcast
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WeatherForecastDTO))]
        public async Task<IActionResult> Generate(CancellationToken ct = default)
        {
            // generate a random record + upsert to DB
            var saved = await _weatherRepository.GenerateNewForecastAsync();

            var dto = saved.MapToWeatherForecastDTO();

            // (optional) real-time broadcast
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", dto, ct);

            return Ok(dto);
        }

        // all (existing)
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherForecastDTO>))]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetAll(CancellationToken ct = default)
        {
            // ⚠️ Here we want EVERYTHING, so we call GetAllAsync() (not GetLatestWeatherForecastAsync)
            var entities = await _weatherRepository.GetAllAsync();
            var dtos = entities.Select(w => w.MapToWeatherForecastDTO()).ToList();
            return Ok(dtos);
        }

        // current via OWM (existing)
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent([FromQuery] string? city = null, CancellationToken ct = default)
        {
            var entity = await _weatherRepository.GetLatestWeatherForecastAsync(ct); 
            if (entity is null) return Ok(Array.Empty<WeatherForecastDTO>());

            return Ok(new[] { entity.MapToWeatherForecastDTO() });
        }

        // history (existing)
        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherForecastDTO>))]
        public async Task<IActionResult> GetHistory([FromQuery, Range(1, 500)] int limit = 10, CancellationToken ct = default)
        {
            // safety terminal (in case the attribute does not apply)
            limit = Math.Clamp(limit, 1, 500);

            var entities = await _weatherRepository.GetHistoryAsync(limit);
            var dtos = entities.Select(w => w.MapToWeatherForecastDTO()).ToList();

            // Always 200, even if empty list (like EventController)
            return Ok(dtos);
        }

        // by id (existing)
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WeatherForecastDTO))]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            if (id <= 0) return BadRequest("The provided ID is invalid.");

            var entity = await _weatherRepository.GetByIdAsync(id);
            if (entity is null) return NotFound($"No events found for the ID {id}.");

            return Ok(entity.MapToWeatherForecastDTO());
        }

        // update + broadcast
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WeatherForecastDTO))]
        public async Task<IActionResult> Update(int id, [FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (id <= 0 || id != dto.Id) return BadRequest("Id mismatch.");

            // DTO -> Entity 
            var entity = dto.MapToWeatherForecast();

            // Upsert 
            var saved = await _weatherRepository.SaveOrUpdateAsync(entity);

            var result = saved.MapToWeatherForecastDTO();

            // Real-time broadcast (reuses the same event as elsewhere)
            await _hubContext.Clients.All.SendAsync("ReceiveForecast", result, ct);

            return Ok(result);
        }


    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.