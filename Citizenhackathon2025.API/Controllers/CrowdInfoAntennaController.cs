using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.DTOs.DTOs.Antennas;
using CitizenHackathon2025.Hubs.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CrowdInfoAntennaController : ControllerBase
    {
        private readonly ICrowdInfoAntennaService _svc;
        private readonly IAntennaCadastreImportService _cadastreImportService;

        public CrowdInfoAntennaController(ICrowdInfoAntennaService svc, IAntennaCadastreImportService cadastreImportService)
        {
            _svc = svc;
            _cadastreImportService = cadastreImportService;
        }

        // GET api/crowdinfoantenna
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _svc.GetAllAsync(ct));

        // GET api/crowdinfoantenna/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var a = await _svc.GetByIdAsync(id, ct);
            return a is null ? NotFound() : Ok(a);
        }

        // GET api/crowdinfoantenna/nearest?lat=..&lng=..&maxRadiusMeters=2000
        [HttpGet("nearest")]
        public async Task<IActionResult> GetNearest([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double maxRadiusMeters = 5000, CancellationToken ct = default)
        {
            var nearest = await _svc.GetNearestAsync(lat, lng, maxRadiusMeters, ct);
            return nearest is null ? NotFound() : Ok(nearest);
        }

        // GET api/crowdinfoantenna/5/counts?windowMinutes=10
        [HttpGet("{id:int}/counts")]
        public async Task<IActionResult> GetCounts(int id, [FromQuery] int windowMinutes = 10, CancellationToken ct = default)
            => Ok(await _svc.GetCountsAsync(id, windowMinutes, ct));

        // GET api/crowdinfoantenna/event/123/crowd?windowMinutes=10&maxRadiusMeters=5000
        [HttpGet("event/{eventId:int}/crowd")]
        public async Task<IActionResult> GetEventCrowd(int eventId, [FromQuery] int windowMinutes = 10, [FromQuery] double maxRadiusMeters = 5000, CancellationToken ct = default)
        {
            var dto = await _svc.GetEventCrowdAsync(eventId, windowMinutes, maxRadiusMeters, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpGet("bounds")]
        public async Task<IActionResult> GetByBounds([FromQuery] double minLat, [FromQuery] double maxLat, [FromQuery] double minLng, [FromQuery] double maxLng, CancellationToken ct = default)
        {
            if (minLat < -90 || maxLat > 90 || minLat > maxLat)
                return BadRequest("Invalid latitude bounds.");

            if (minLng < -180 || maxLng > 180 || minLng > maxLng)
                return BadRequest("Invalid longitude bounds.");

            var result = await _svc.GetByBoundsAsync(minLat, maxLat, minLng, maxLng, ct);
            return Ok(result);
        }

        [HttpGet("debug-sql")]
        public async Task<IActionResult> DebugSql([FromServices] IDbConnection db, CancellationToken ct)
        {
            const string sql = """
                SELECT
                    @@SERVERNAME AS ServerName,
                    DB_NAME() AS CurrentDatabase,
                    SUSER_SNAME() AS LoginName,
                    (
                        SELECT COUNT(*)
                        FROM dbo.CrowdInfoAntenna
                    ) AS AntennaCount;
                """;

            var result = await db.QuerySingleAsync(
                new CommandDefinition(sql, cancellationToken: ct));

            return Ok(result);
        }

        // POST api/crowdinfoantenna
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCrowdInfoAntennaDTO dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var created = await _svc.CreateAntennaAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("import-cadastre")]
        public async Task<IActionResult> ImportCadastre(CancellationToken ct)
        {
            var processed = await _cadastreImportService.ImportAsync(ct);

            return Ok(new CadastreImportResultDTO
            {
                Processed = processed,
                SyncedAtUtc = DateTime.UtcNow
            });
        }
        // DELETE api/crowdinfoantenna/5
        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> Delete(int id, CancellationToken ct)
        //{
        //    var deleted = await _svc.DeleteAntennaAsync(id, ct);

        //    if (!deleted)
        //        return NotFound();

        //    return NoContent();
        //}
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.