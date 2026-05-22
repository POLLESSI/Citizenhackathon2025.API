using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/crowd/safety-alerts")]
    public sealed class CrowdSafetyAlertController : ControllerBase
    {
        private readonly ICrowdSafetyAlertRepository _repo;

        public CrowdSafetyAlertController(ICrowdSafetyAlertRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("latest")]
        public async Task<IActionResult> Latest([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var rows = await _repo.GetLatestAsync(limit, ct);

            var dtos = rows.Select(a => new CrowdSafetyAlertDTO
            {
                Id = a.Id,
                AntennaId = a.AntennaId,
                EventId = a.EventId,
                Severity = a.Severity,
                Status = a.Status,
                ActiveConnections = a.ActiveConnections,
                UniqueDevices = a.UniqueDevices,
                BaselineConnections = a.BaselineConnections,
                IsRural = a.IsRural,
                IsNight = a.IsNight,
                IsKnownEvent = a.IsKnownEvent,
                IsSensitiveZone = a.IsSensitiveZone,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Title = a.Title,
                Message = a.Message,
                DetectedAtUtc = a.DetectedAtUtc,
                ValidatedAtUtc = a.ValidatedAtUtc,
                ValidatedByUserId = a.ValidatedByUserId,
                Active = a.Active
            });

            var alerts = await _repo.GetLatestAsync(limit, ct);

            return Ok(dtos);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [Authorize(Policy = Policies.ModoPolicy)]
        [HttpPost("{id:long}/validate")]
        public async Task<IActionResult> Validate(long id, CancellationToken ct = default)
        {
            var n = await _repo.ValidateAsync(id, validatedByUserId: 1, ct);
            return n > 0 ? NoContent() : NotFound();
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.