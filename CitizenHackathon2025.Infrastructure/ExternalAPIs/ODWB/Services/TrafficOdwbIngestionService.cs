using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.Helpers;
using CitizenHackathon2025.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services
{
    public sealed class TrafficOdwbIngestionService : ITrafficOdwbIngestionService
    {
        private readonly IOdwbTrafficApiService _odwb;
        private readonly ITrafficConditionRepository _repo;
        private readonly TrafficApiOptions _opt;
        private readonly ILogger<TrafficOdwbIngestionService> _log;

        public TrafficOdwbIngestionService(
            IOdwbTrafficApiService odwb,
            ITrafficConditionRepository repo,
            IOptions<TrafficApiOptions> opt,
            ILogger<TrafficOdwbIngestionService> log)
            => (_odwb, _repo, _opt, _log) = (odwb, repo, opt.Value, log);

        public async Task<int> SyncAsync(int? limit = null, CancellationToken ct = default)
        {
            var q = new OdwbQuery(
                Select: null,
                Where: null,
                OrderBy: "periode desc",
                Limit: limit ?? _opt.DefaultLimit
            );

            var resp = await _odwb.QueryAsync(q, ct);
            if (resp.Results.Count == 0)
            {
                _log.LogWarning("ODWB sync: no results");
                return 0;
            }

            var now = DateTime.UtcNow;
            var upserted = 0;

            foreach (var r in resp.Results)
            {
                // Dataset 217400: typical fields
                var entite = TryString(r, "entite") ?? "ODWB";
                var periode = TryString(r, "periode") ?? "";
                var total = TryInt(r, "nombre_d_accidents_de_la_circulation_total");

                // We create a proxy "TrafficCondition" (not a live incident).
                var dtoLat = TryGeoLat(r) ?? 50.0m;
                var dtoLon = TryGeoLon(r) ?? 4.0m;

                var dateCondition = now; // annual dataset => no live timestamp

                var provider = "odwb";
                var incidentType = total is null
                    ? $"ODWB accidents {entite} {periode}"
                    : $"ODWB accidents {entite} {periode} total={total}";

                var congestion = total is null ? "N/A" : (total.Value >= 20 ? "4" : total.Value >= 10 ? "3" : "2");

                var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                    provider: provider,
                    lat: dtoLat,
                    lon: dtoLon,
                    dateUtc: dateCondition,
                    incidentType: incidentType,
                    location: entite,
                    congestionLevel: congestion,
                    timeBucket: TimeSpan.FromHours(24) // annual dataset => bucket large
                );

                var tc = new TrafficCondition
                {
                    Latitude = dtoLat,
                    Longitude = dtoLon,
                    DateCondition = dateCondition,
                    CongestionLevel = congestion,
                    IncidentType = incidentType,

                    Provider = provider,
                    ExternalId = externalId,
                    Fingerprint = fingerprint,
                    LastSeenAt = now,

                    Title = incidentType,
                    Road = entite
                };

                var saved = await _repo.UpsertTrafficConditionAsync(tc);
                if (saved is not null) upserted++;
            }

            _log.LogInformation("ODWB sync done: {Count} upserted", upserted);
            return upserted;
        }

        private static string? TryString(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static int? TryInt(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var i) ? i : null;

        private static decimal? TryGeoLat(Dictionary<string, object?> r)
        {
            // ODWB often returns geo_point_2d: { lon, lat }
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;
            // v can be JsonElement
            if (v is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (je.TryGetProperty("lat", out var lat) && lat.TryGetDecimal(out var d)) return d;
            }
            return null;
        }

        private static decimal? TryGeoLon(Dictionary<string, object?> r)
        {
            if (!r.TryGetValue("geo_point_2d", out var v) || v is null) return null;
            if (v is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (je.TryGetProperty("lon", out var lon) && lon.TryGetDecimal(out var d)) return d;
            }
            return null;
        }
    }

}
