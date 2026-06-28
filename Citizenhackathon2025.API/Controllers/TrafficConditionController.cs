using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.Helpers;
using CitizenHackathon2025.Infrastructure.Repositories;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Data;
using HubEvents = CitizenHackathon2025.Contracts.Hubs.TrafficConditionHubMethods;

namespace CitizenHackathon2025.API.Controllers
{
    [EnableRateLimiting("per-user")]
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficConditionController : ControllerBase
    {
        private readonly ITrafficConditionRepository _trafficConditionRepository;
        private readonly ICriticalAlertQuorumService _criticalAlertQuorumService;
        private readonly ITrafficApiService _trafficApiService;
        private readonly IHubContext<TrafficHub> _hubContext;
        private readonly ILogger<TrafficConditionController> _logger;
        private readonly byte[] _trafficHmacKey;

        private const string HubMethod_ReceiveTrafficConditionUpdate = "ReceiveTrafficConditionUpdate";

        public TrafficConditionController(ITrafficConditionRepository trafficConditionRepository, ICriticalAlertQuorumService criticalAlertQuorumService, ITrafficApiService trafficApiService, IHubContext<TrafficHub> hubContext, ILogger<TrafficConditionController> logger, byte[] trafficHmacKey)
        {
            _trafficConditionRepository = trafficConditionRepository;
            _criticalAlertQuorumService = criticalAlertQuorumService;
            _trafficApiService = trafficApiService;
            _hubContext = hubContext;
            _logger = logger;
            _trafficHmacKey = trafficHmacKey;
        }

        // 1) Endpoint to retrieve the latest in the database
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestTrafficCondition(CancellationToken ct)
        {
            await _trafficConditionRepository.ArchivePastTrafficConditionsAsync(ct);

            var trafficConditions = await _trafficConditionRepository
                .GetLatestTrafficConditionAsync(limit: 10, ct: ct);

            return Ok(trafficConditions.Select(tc => tc.MapToTrafficConditionDTO()).ToList());
        }
        // 2) Endpoint for live fetch from Odwb
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(double lat, double lon, CancellationToken ct)
        {
            var dto = await _trafficApiService.GetCurrentTrafficAsync(lat, lon, ct);
            if (dto is null)
            {
                _logger.LogWarning("ODWB returned null");
                return NotFound();
            }

            // Ensures UTC (important for SQL + consistency)
            var dateUtc = dto.DateCondition.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.DateCondition, DateTimeKind.Utc)
                : dto.DateCondition.ToUniversalTime();

            var provider = "odwb"; // or "waze" if it really is Waze, otherwise naming consistency

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

        [HttpGet("by-congestionlevel")]
        public async Task<IActionResult> GetByCongestionLevel([FromQuery] string congestionLevel, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(congestionLevel))
                return BadRequest("congestionLevel is required.");

            congestionLevel = congestionLevel.Trim();

            if (congestionLevel.Length < 2)
                return BadRequest("congestionLevel must contain at least 2 characters.");

            congestionLevel = TrafficCongestionHelper.NormalizeCongestionLevelQuery(congestionLevel);

            var entities = await _trafficConditionRepository
                .GetByCongestionLevelAsync(congestionLevel, ct);

            var dtos = entities
                .Select(tc => tc.MapToTrafficConditionDTO())
                .ToList();

            return Ok(dtos);
        }

        [HttpGet("by-incidenttype")]
        public async Task<IActionResult> GetByIncidentType([FromQuery] string incidentType, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(incidentType))
                return BadRequest("incidentType is required.");

            incidentType = incidentType.Trim();

            if (incidentType.Length < 2)
                return BadRequest("incidentType must contain at least 2 characters.");

            var entities = await _trafficConditionRepository
                .GetByIncidentTypeAsync(incidentType, ct);

            var dtos = entities
                .Select(tc => tc.MapToTrafficConditionDTO())
                .ToList();

            return Ok(dtos);
        }

        [HttpGet("by-location")]
        public async Task<IActionResult> GetByLocation([FromQuery] string location, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest("location is required.");

            location = location.Trim();

            if (location.Length < 2)
                return BadRequest("location must contain at least 2 characters.");

            var entities = await _trafficConditionRepository
                .GetByLocationAsync(location, ct);

            var dtos = entities
                .Select(tc => tc.MapToTrafficConditionDTO())
                .ToList();

            return Ok(dtos);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("test-di")]
        public IActionResult TestDi()
        {
            return Ok(_trafficApiService.GetType().Name); // Must display "TrafficApiService"
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("test-getbyid/{id}")]
        public async Task<IActionResult> TestGetById([FromServices] ITrafficConditionService service, int id)
        {
            var tc = await service.GetByIdAsync(id);
            return tc == null ? NotFound() : Ok(tc);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("debug-proc-traffic")]
        public async Task<IActionResult> DebugProcTraffic([FromServices] IDbConnection cn)
        {
            const string sql = @"
                            SELECT
                                DB_NAME() AS DbName,
                                p.parameter_id,
                                p.name,
                                TYPE_NAME(p.user_type_id) AS TypeName
                            FROM sys.parameters p
                            WHERE p.object_id = OBJECT_ID('dbo.sp_TrafficCondition_Upsert')
                            ORDER BY p.parameter_id;";

            var rows = await cn.QueryAsync(sql);
            return Ok(rows);
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("db-info")]
        public async Task<IActionResult> DbInfo([FromServices] IDbConnection cn)
        {
            var db = await cn.ExecuteScalarAsync<string>("SELECT DB_NAME();");
            return Ok(new
            {
                Database = db,
                DataSource = cn.ConnectionString
            });
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpGet("debug-odwb-raw")]
        public async Task<IActionResult> DebugOdwRaw([FromServices] IHttpClientFactory factory, [FromServices] IConfiguration config, [FromQuery] int limit = 5, CancellationToken ct = default)
        {
            var baseUrl = config["ExternalProviders:ODWB:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                return Problem("ExternalProviders:ODWB:BaseUrl is missing.");

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                return BadRequest($"Invalid ODWB BaseUrl: {baseUrl}");

            var separator = baseUrl.Contains('?') ? "&" : "?";
            var url = $"{baseUrl.TrimEnd('/')}{separator}limit={Math.Clamp(limit, 1, 100)}";

            try
            {
                var http = factory.CreateClient("ODWB");

                using var response = await http.GetAsync(url, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                return StatusCode((int)response.StatusCode, new
                {
                    Url = url,
                    StatusCode = (int)response.StatusCode,
                    response.ReasonPhrase,
                    Body = body
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new
                {
                    Error = "ODWB HTTP call failed.",
                    Url = url,
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        [Authorize(Policy = "AdminOrModo")]
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
            TrafficUpsertIdentityHmac.Ensure(entity, defaultProvider: "manual", hmacKey: key, timeBucket: TimeSpan.FromMinutes(1));

            var saved = await _trafficConditionRepository.UpsertTrafficConditionAsync(entity);
            if (saved is null) return Problem("UPSERT failed");

            var savedDto = saved.MapToTrafficConditionDTO();

            await _hubContext.Clients.All.SendAsync(HubEvents.ToClient.TrafficUpdated, savedDto, ct);

            return Ok(savedDto);
        }

        [Authorize(Policy = "AdminOrModo")]
        [HttpPost("sync-odwb")]
        public async Task<IActionResult> SyncOdwb([FromServices] ITrafficOdwbIngestionService svc, [FromQuery] int? limit, CancellationToken ct)
        {
            var upserted = await svc.SyncAsync(limit, ct);

            return Ok(new
            {
                ServiceType = svc.GetType().FullName,
                Limit = limit,
                Upserted = upserted
            });
        }

        [Authorize(Policy = Policies.UserPolicy)]
        [HttpPost("manual-critical-alert")]
        public async Task<ActionResult<TrafficAlertResultDTO>> ManualCriticalTrafficAlert([FromBody] ManualTrafficAlertDTO request, CancellationToken ct)
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest("Invalid coordinates.");
            }

            if (request.TrafficLevel < TrafficLevel.Heavy)
                return BadRequest("Manual critical traffic alert requires Heavy or Jammed level.");

            var quorum = await _criticalAlertQuorumService.RegisterVoteAsync(CriticalAlertKind.Traffic, null, request.Latitude, request.Longitude, request.DeviceId, request.Description, ct);

            if (!quorum.Confirmed)
            {
                return Ok(new TrafficAlertResultDTO
                {
                    Ok = true,
                    Status = "Pending",
                    ConfirmationCount = quorum.ConfirmationCount,
                    RequiredCount = quorum.RequiredCount
                });
            }

            var nowUtc = DateTime.UtcNow;

            var dto = new TrafficConditionDTO
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                DateCondition = nowUtc,
                CongestionLevel = ((int)request.TrafficLevel).ToString(),
                IncidentType = request.IncidentType,
                Message = request.Description,
                Level = (byte)request.TrafficLevel
            };

            var dateUtc = dto.DateCondition.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.DateCondition, DateTimeKind.Utc)
                : dto.DateCondition.ToUniversalTime();

            var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                provider: "manual",
                lat: dto.Latitude,
                lon: dto.Longitude,
                dateUtc: dateUtc,
                incidentType: dto.IncidentType,
                location: dto.Location,
                congestionLevel: dto.CongestionLevel,
                timeBucket: TimeSpan.FromMinutes(1));

            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dateUtc,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType,

                Provider = "manual",
                ExternalId = externalId,
                Fingerprint = fingerprint,
                LastSeenAt = nowUtc,

                Title = dto.Message,
                Road = dto.Location,
                Severity = dto.Level,
                Active = true
            };

            TrafficUpsertIdentityHmac.Ensure(
                entity,
                defaultProvider: "manual",
                hmacKey: _trafficHmacKey,
                timeBucket: TimeSpan.FromMinutes(1));

            var saved = await _trafficConditionRepository.UpsertTrafficConditionAsync(entity);
            if (saved is null)
            {
                return Ok(new TrafficAlertResultDTO
                {
                    Ok = false,
                    Status = "Error",
                    Error = "UPSERT failed"
                });
            }

            var savedDto = saved.MapToTrafficConditionDTO();

            await _hubContext.Clients.All.SendAsync(
                HubEvents.ToClient.TrafficUpdated,
                savedDto,
                ct);

            return Ok(new TrafficAlertResultDTO
            {
                Ok = true,
                Status = "Confirmed",
                ConfirmationCount = quorum.ConfirmationCount,
                RequiredCount = quorum.RequiredCount,
                ExpiresAtUtc = nowUtc.AddMinutes(5)
            });
        }

        [Authorize(Policy = Policies.AdminPolicy)]
        [HttpPost("archive-expired")]
        public async Task<IActionResult> ArchiveExpiredTrafficConditions(CancellationToken ct)
        {
            var archived = await _trafficConditionRepository.ArchivePastTrafficConditionsAsync(ct);
            return Ok(new { ArchivedCount = archived });
        }

        [Authorize(Policy = "AdminOrModo")]
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