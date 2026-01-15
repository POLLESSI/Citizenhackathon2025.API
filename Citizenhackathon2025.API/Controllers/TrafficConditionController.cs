using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.Helpers;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.TrafficConditionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficConditionController : ControllerBase
    {
        private readonly ITrafficConditionRepository _trafficConditionRepository;
        private readonly ITrafficApiService _trafficApiService;
        private readonly IHubContext<TrafficHub> _hubContext;
        private readonly byte[] _trafficHmacKey;

        private const string HubMethod_ReceiveTrafficConditionUpdate = "ReceiveTrafficConditionUpdate";

        public TrafficConditionController(ITrafficConditionRepository trafficConditionRepository, ITrafficApiService trafficApiService, IHubContext<TrafficHub> hubContext, byte[] trafficHmacKey)
        {
            _trafficConditionRepository = trafficConditionRepository;
            _trafficApiService = trafficApiService;
            _hubContext = hubContext;
            _trafficHmacKey = trafficHmacKey;
        }

        // 1) Endpoint to retrieve the latest in the database
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestTrafficCondition(CancellationToken ct)
        {
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync(limit: 10, ct: ct);
            var dtos = trafficConditions.Select(tc => tc?.MapToTrafficConditionDTO()).ToList();
            return Ok(dtos);
        }
        // 2) Endpoint for live fetch from Odwb
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(double lat, double lon, CancellationToken ct)
        {
            var dto = await _trafficApiService.GetCurrentTrafficAsync(lat, lon, ct);
            if (dto is null) return NotFound();

            // Ensures UTC (important for SQL + consistency)
            var dateUtc = dto.DateCondition.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.DateCondition, DateTimeKind.Utc)
                : dto.DateCondition.ToUniversalTime();

            var provider = "trafficapi"; // or "waze" if it really is Waze, otherwise naming consistency

            // ExternalId stable + fingerprint stable
            var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                provider: provider,
                lat: dto.Latitude,
                lon: dto.Longitude,
                dateUtc: dateUtc,
                incidentType: dto.IncidentType,
                location: dto.Location,
                congestionLevel: dto.CongestionLevel,
                timeBucket: TimeSpan.FromMinutes(1) // adjust if needed
            );

            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dateUtc,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType,

                Provider = provider,
                ExternalId = externalId,
                Fingerprint = fingerprint,
                LastSeenAt = DateTime.UtcNow,

                // Optional if you want to use your DB columns :
                Title = dto.Message,
                Road = dto.Location,
                // Severity = ... if you have a logic
            };

            var saved = await _trafficConditionRepository.UpsertTrafficConditionAsync(entity);
            if (saved is null) return Problem("UPSERT failed");

            var dtoOut = saved.MapToTrafficConditionDTO();

            await _hubContext.Clients.All.SendAsync(HubEvents.ToClient.TrafficUpdated, dtoOut, ct);

            return Ok(dtoOut);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTrafficConditionById(int id)
        {
            if (id <= 0) return BadRequest("The provided ID is invalid.");
            var trafficCondition = await _trafficConditionRepository.GetByIdAsync(id);
            if (trafficCondition == null || !trafficCondition.Active)
            {
                return NotFound($"TrafficCondition with ID {id} not found or inactive.");
            }

            return Ok(trafficCondition.MapToTrafficConditionDTO());
        }
        [HttpGet("test-di")]
        public IActionResult TestDi()
        {
            return Ok(_trafficApiService.GetType().Name); // Must display "TrafficApiService"
        }
        [HttpGet("test-getbyid/{id}")]
        public async Task<IActionResult> TestGetById([FromServices] ITrafficConditionService service, int id)
        {
            var tc = await service.GetByIdAsync(id);
            return tc == null ? NotFound() : Ok(tc);
        }
        [HttpPost]
        public async Task<IActionResult> SaveTrafficCondition([FromBody] TrafficConditionDTO dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var dateUtc = dto.DateCondition == default
                ? DateTime.UtcNow
                : (dto.DateCondition.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dto.DateCondition, DateTimeKind.Utc)
                    : dto.DateCondition.ToUniversalTime());

            var provider = "manual";

            // For a manual POST: you can choose stable (hash) OR unique (GUID)
            // Stable is better if you want to "update the same incident".
            var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                provider: provider,
                lat: dto.Latitude,
                lon: dto.Longitude,
                dateUtc: dateUtc,
                incidentType: dto.IncidentType,
                location: dto.Location,
                congestionLevel: dto.CongestionLevel,
                timeBucket: TimeSpan.FromMinutes(1)
            );

            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition == default ? DateTime.UtcNow : dto.DateCondition.ToUniversalTime(),
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType,

                Provider = "manual",
                ExternalId = externalId,
                Fingerprint = fingerprint,
                LastSeenAt = DateTime.UtcNow,

                Title = dto.Message,
                Road = dto.Location
            };

            // ✅ Load the key from IConfiguration/IOptions
            var key = _trafficHmacKey; // see §6
            TrafficUpsertIdentityHmac.Ensure(entity, defaultProvider: "odwb", hmacKey: key, timeBucket: TimeSpan.FromMinutes(1));

            var saved = await _trafficConditionRepository.UpsertTrafficConditionAsync(entity);
            if (saved is null) return Problem("UPSERT failed");

            var savedDto = saved.MapToTrafficConditionDTO();
            await _hubContext.Clients.All.SendAsync(HubEvents.ToClient.TrafficUpdated, savedDto, ct);
            return Ok(savedDto);
        }
        [HttpPost("sync-odwb")]
        public async Task<IActionResult> SyncOdwb([FromServices] ITrafficOdwbIngestionService svc, int? limit, CancellationToken ct)
        {
            var n = await svc.SyncAsync(limit, ct);
            return Ok(new { Upserted = n });
        }

        [HttpPost("archive-expired")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ArchiveExpiredTrafficConditions()
        {
            var archived = await _trafficConditionRepository.ArchivePastTrafficConditionsAsync();
            return Ok(new { ArchivedCount = archived });
        }
        [HttpPut("{id:int}")]
        public IActionResult UpdateTrafficCondition(int id, [FromBody] TrafficConditionUpdateDTO dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch");

            var entity = dto.MapToEntity();                // ✅ WithId(dto.Id)
            var result = _trafficConditionRepository.UpdateTrafficCondition(entity);

            return result != null ? Ok(result) : NotFound();
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.