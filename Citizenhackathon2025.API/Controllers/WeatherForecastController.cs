using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastAppService _app;

        public WeatherForecastController(IWeatherForecastAppService app)
            => _app = app;

        [HttpPost]
        public async Task<ActionResult<WeatherForecastDTO>> Create([FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto is null) return BadRequest();
            var created = await _app.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("manual")]
        public async Task<ActionResult<WeatherForecastDTO>> Manual([FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto is null) return BadRequest();
            return Ok(await _app.ManualAsync(dto, ct));
        }

        [HttpPost("pull")]
        public async Task<IActionResult> Pull([FromQuery] decimal lat, [FromQuery] decimal lon, CancellationToken ct = default)
        {
            if (lat is < -90 or > 90) return BadRequest("Invalid lat.");
            if (lon is < -180 or > 180) return BadRequest("Invalid lon.");

            var (alertsUpserted, forecastSaved) = await _app.PullAsync(lat, lon, ct);
            return Ok(new { alertsUpserted, forecastSaved });
        }

        [HttpPost("generate")]
        public async Task<ActionResult<WeatherForecastDTO>> Generate(CancellationToken ct = default)
            => Ok(await _app.GenerateAsync(ct));

        [HttpGet("all")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetAll(CancellationToken ct = default)
            => Ok(await _app.GetAllAsync(ct));

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent([FromQuery] string? city = null, CancellationToken ct = default)
        {
            var arr = await _app.GetCurrentAsync(city, ct);
            // WHY: eep customer compatibility: always table
            return Ok(arr);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery, Range(1, 500)] int limit = 10, CancellationToken ct = default)
            => Ok(await _app.GetHistoryAsync(limit, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            if (id <= 0) return BadRequest("Invalid id.");

            var dto = await _app.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("archive-expired")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ArchiveExpired(CancellationToken ct = default)
            => Ok(new { ArchivedCount = await _app.ArchiveExpiredAsync(ct) });

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto is null) return BadRequest("Body is required.");
            if (id <= 0 || id != dto.Id) return BadRequest("Id mismatch.");

            return Ok(await _app.UpdateAsync(id, dto, ct));
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.