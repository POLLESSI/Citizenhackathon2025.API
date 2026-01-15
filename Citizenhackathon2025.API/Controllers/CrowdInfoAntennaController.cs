using CitizenHackathon2025.Hubs.Services;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CrowdInfoAntennaController : ControllerBase
    {
        private readonly ICrowdInfoAntennaService _svc;

        public CrowdInfoAntennaController(ICrowdInfoAntennaService svc) => _svc = svc;

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
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.