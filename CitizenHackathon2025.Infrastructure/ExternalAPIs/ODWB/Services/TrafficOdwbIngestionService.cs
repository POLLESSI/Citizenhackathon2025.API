using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.ExternalProviders;
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
        private readonly ITrafficConditionNormalizer _normalizer;
        private readonly TrafficApiOptions _opt;
        private readonly ILogger<TrafficOdwbIngestionService> _log;

        public TrafficOdwbIngestionService(IOdwbTrafficApiService odwb, ITrafficConditionRepository repo, IOptions<TrafficApiOptions> opt, ILogger<TrafficOdwbIngestionService> log, ITrafficConditionNormalizer normalizer)
        {
            _odwb = odwb;
            _repo = repo;
            _opt = opt.Value;
            _log = log;
            _normalizer = normalizer;
        }

        public async Task<int> SyncAsync(int? limit = null, CancellationToken ct = default)
        {
            var q = new OdwbQuery(
                Select: null,
                Where: null,
                OrderBy: null,
                Limit: limit ?? _opt.DefaultLimit
            );

            var resp = await _odwb.QueryAsync(q, ct);
            _log.LogInformation(
                "ODWB sync fetched TotalCount={TotalCount}, Results={ResultsCount}, Limit={Limit}",
                resp.TotalCount,
                resp.Results.Count,
                q.Limit);
            if (resp.Results.Count == 0)
            {
                _log.LogWarning("ODWB sync: no results");
                return 0;
            }

            var now = DateTime.UtcNow;
            var upserted = 0;

            foreach (var r in resp.Results)
            {
                ct.ThrowIfCancellationRequested();

                var entite = TryString(r, "entite") ?? "ODWB";
                var periode = TryString(r, "periode") ?? "";
                var ins = TryString(r, "ins") ?? entite;

                var total = TryInt(r, "nombre_d_accidents_de_la_circulation_total");

                var lat = TryGeoLat(r) ?? 50.0m;
                var lon = TryGeoLon(r) ?? 4.0m;

                var severity = total switch
                {
                    >= 50 => (byte)4,
                    >= 25 => (byte)3,
                    >= 10 => (byte)2,
                    _ => (byte)1
                };

                var congestion = severity switch
                {
                    4 => "4",
                    3 => "3",
                    2 => "2",
                    _ => "1"
                };

                var provider = "odwb-walstat";
                var incidentType = "AccidentRiskStatistics";
                var title = $"Risque accidentologie {entite}";
                var road = entite;

                var dateCondition = now;

                var externalIdRaw = $"odwb-walstat-217400-{TryString(r, "ins")}-{periode}";

                var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                    provider: provider,
                    lat: lat,
                    lon: lon,
                    dateUtc: dateCondition,
                    incidentType: incidentType,
                    location: entite,
                    congestionLevel: congestion,
                    timeBucket: TimeSpan.FromHours(24)
                );

                var tc = new TrafficCondition
                {
                    Latitude = lat,
                    Longitude = lon,
                    DateCondition = dateCondition,

                    CongestionLevel = congestion,
                    IncidentType = "AccidentRiskStatistics",

                    Provider = "odwb-walstat",
                    ExternalId = externalId,
                    Fingerprint = fingerprint,
                    LastSeenAt = now,

                    Title = $"Risque accidentologie {entite}",
                    Road = entite,
                    Severity = severity,
                    Active = true

                    //Provider = "perex";
                    //IncidentType = "LiveTrafficIncident";
                    //Title = perexEvent.Title;
                    //Road = perexEvent.RoadName;
                };

                _normalizer.Normalize(tc);

                var saved = await _repo.UpsertTrafficConditionAsync(tc);
                if (saved is not null)
                    upserted++;
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
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.