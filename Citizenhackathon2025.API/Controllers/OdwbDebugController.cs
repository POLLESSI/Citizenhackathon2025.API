using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CitizenHackathon2025.API.Controllers
{
    [ApiController]
    [Route("api/odwb")]
    public sealed class OdwbDebugController : ControllerBase
    {
        private readonly IOdwbTrafficApiService _odwb;
        private readonly ITrafficConditionRepository _repo;
        private readonly ILogger<OdwbDebugController> _log;

        public OdwbDebugController(
            IOdwbTrafficApiService odwb,
            ITrafficConditionRepository repo,
            ILogger<OdwbDebugController> log)
            => (_odwb, _repo, _log) = (odwb, repo, log);

        [HttpGet("ping")]
        public async Task<IActionResult> Ping(CancellationToken ct)
        {
            var res = await _odwb.QueryAsync(new OdwbQuery(Limit: 1), ct);
            return Ok(new { Count = res?.Results?.Count ?? 0 });
        }

        [HttpGet("odwb/test")]
        public async Task<IActionResult> TestOdwb(CancellationToken ct)
        {
            var r = await _odwb.QueryAsync(new OdwbQuery(Limit: 1), ct);
            return Ok(r);
        }

        // ✅ NEW: ODWB -> DB (UPSERT)
        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromQuery] int limit = 10, CancellationToken ct = default)
        {
            var resp = await _odwb.QueryAsync(new OdwbQuery(Limit: limit), ct);
            if (resp.Results.Count == 0) return Ok(new { Upserted = 0 });

            var now = DateTime.UtcNow;
            var upserted = 0;

            foreach (var r in resp.Results)
            {
                // ODWB record is Dictionary<string, object?>
                var entite = TryString(r, "entite") ?? "ODWB";
                var periode = TryString(r, "periode") ?? "";
                var total = TryInt(r, "nombre_d_accidents_de_la_circulation_total");

                var lat = TryGeoLat(r) ?? 50.0m;
                var lon = TryGeoLon(r) ?? 4.0m;

                var incidentType = total is null
                    ? $"ODWB accidents {entite} {periode}"
                    : $"ODWB accidents {entite} {periode} total={total}";

                var congestion = total is null ? "N/A" : (total.Value >= 20 ? "4" : total.Value >= 10 ? "3" : "2");

                var provider = "odwb";

                var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                    provider: provider,
                    lat: lat,
                    lon: lon,
                    dateUtc: now,                  // Annual dataset: no live incident date
                    incidentType: incidentType,
                    location: entite,
                    congestionLevel: congestion,
                    timeBucket: TimeSpan.FromHours(24)
                );

                var tc = new TrafficCondition
                {
                    Latitude = lat,
                    Longitude = lon,
                    DateCondition = now,
                    CongestionLevel = congestion,
                    IncidentType = incidentType,

                    Provider = provider,
                    ExternalId = externalId,
                    Fingerprint = fingerprint,
                    LastSeenAt = now,

                    Title = incidentType,
                    Road = entite,
                    Active = true
                };

                var saved = await _repo.UpsertTrafficConditionAsync(tc);
                if (saved is not null) upserted++;
            }

            _log.LogInformation("ODWB sync: upserted={Count}", upserted);
            return Ok(new { Upserted = upserted });
        }

        private static string? TryString(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static int? TryInt(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var i) ? i : null;

        private static decimal? TryGeoLat(Dictionary<string, object?> r)
        {
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Object)
                if (je.TryGetProperty("lat", out var lat) && lat.TryGetDecimal(out var d)) return d;

            return null;
        }

        private static decimal? TryGeoLon(Dictionary<string, object?> r)
        {
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Object)
                if (je.TryGetProperty("lon", out var lon) && lon.TryGetDecimal(out var d)) return d;

            return null;
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.