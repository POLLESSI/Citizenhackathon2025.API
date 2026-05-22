using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Data;

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

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost]
        public async Task<ActionResult<WeatherForecastDTO>> Create([FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto is null) return BadRequest();
            var created = await _app.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost("manual")]
        public async Task<ActionResult<WeatherForecastDTO>> Manual([FromBody] WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto is null) return BadRequest();
            return Ok(await _app.ManualAsync(dto, ct));
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost("pull")]
        public async Task<IActionResult> Pull([FromQuery] decimal lat, [FromQuery] decimal lon, CancellationToken ct)
        {
            var (alertsUpserted, forecastSaved) = await _app.PullAsync(lat, lon, ct);

            return Ok(new
            {
                Success = true,
                Source = "OpenWeather",
                Latitude = lat,
                Longitude = lon,
                Forecast = forecastSaved,
                AlertsUpserted = alertsUpserted
            });
        }

        [Authorize(Policy = Policies.UserPolicy)]
        [HttpPost("pull-current-location")]
        public async Task<IActionResult> PullCurrentLocation([FromQuery] decimal lat, [FromQuery] decimal lon, CancellationToken ct)
        {
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                return BadRequest("Invalid coordinates.");

            var (alertsUpserted, forecastSaved) = await _app.PullAsync(lat, lon, ct);

            return Ok(new
            {
                Success = true,
                Source = "OpenWeather",
                Forecast = forecastSaved,
                AlertsUpserted = alertsUpserted
            });
        }

        //[HttpPost("generate")]
        //public async Task<ActionResult<WeatherForecastDTO>> Generate(CancellationToken ct = default)
        //    => Ok(await _app.GenerateAsync(ct));

        [HttpGet("all")]
        public async Task<ActionResult<List<WeatherForecastDTO>>> GetAll(CancellationToken ct = default)
        {
            await _app.ArchiveExpiredAsync(ct);
            return Ok(await _app.GetAllAsync(ct));
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent([FromQuery] string? city = null, CancellationToken ct = default)
        {
            var arr = await _app.GetCurrentAsync(city, ct);
            // WHY: eep customer compatibility: always table
            return Ok(arr);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery, Range(1, 500)] int limit = 10, CancellationToken ct = default)
        {
            await _app.ArchiveExpiredAsync(ct);
            return Ok(await _app.GetHistoryAsync(limit, ct));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            if (id <= 0) return BadRequest("Invalid id.");

            var dto = await _app.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("debug-db-weather")]
        public async Task<IActionResult> DebugDbWeather([FromServices] IDbConnection cn)
        {
            const string sql = @"
                            SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DbName;

                            SELECT COUNT(*) AS WeatherRows FROM dbo.WeatherForecast;

                            SELECT 
                                p.parameter_id,
                                p.name,
                                TYPE_NAME(p.user_type_id) AS TypeName
                            FROM sys.parameters p
                            WHERE p.object_id = OBJECT_ID('dbo.sp_WeatherForecast_Upsert')
                            ORDER BY p.parameter_id;";

            using var multi = await cn.QueryMultipleAsync(sql);

            return Ok(new
            {
                Db = await multi.ReadFirstAsync(),
                Count = await multi.ReadFirstAsync(),
                Parameters = await multi.ReadAsync()
            });
        }

        //[Authorize(Policy = Policies.AdminPolicy)]
        //[HttpGet("db-info")]
        //public IActionResult GetDbInfo([FromServices] IDbConnection cn)
        //{
        //    return Ok(new
        //    {
        //        Database = cn.Database,
        //        ConnectionString = cn.ConnectionString
        //    });
        //}

        //[HttpGet("debug-proc-weather")]
        //public async Task<IActionResult> DebugProcWeather([FromServices] IDbConnection cn)
        //{
        //    const string sql = @"
        //                    SELECT 
        //                        DB_NAME() AS DbName,
        //                        p.parameter_id,
        //                        p.name,
        //                        TYPE_NAME(p.user_type_id) AS TypeName
        //                    FROM sys.parameters p
        //                    WHERE p.object_id = OBJECT_ID('dbo.sp_WeatherForecast_Upsert')
        //                    ORDER BY p.parameter_id;";

        //    var rows = await cn.QueryAsync(sql);
        //    return Ok(rows);
        //}

        [HttpPost("archive-expired")]
        [Authorize(Policy = Policies.AdminPolicy)]
        public async Task<IActionResult> ArchiveExpired(CancellationToken ct = default)
            => Ok(new { ArchivedCount = await _app.ArchiveExpiredAsync(ct) });

        [Authorize(Policy = "AdminOrModo")]
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