using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Adapters
{
    public sealed class OdwbTrafficApiAdapter : ITrafficApiService
    {
        private readonly IOdwbTrafficApiService _odwb;
        private readonly ILogger<OdwbTrafficApiAdapter> _log;

        public OdwbTrafficApiAdapter(IOdwbTrafficApiService odwb, ILogger<OdwbTrafficApiAdapter> log)
            => (_odwb, _log) = (odwb, log);

        public async Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude, CancellationToken ct = default)
        {
            // Here: ODWB dataset 217400 = accident stats (annual period), not "live incident".
            // So: we return a "pseudo-traffic" DTO (or you change the dataset).

            var q = new OdwbQuery(
                Where: null,
                Select: null,
                OrderBy: "periode desc",
                Limit: 1
            );

            var resp = await _odwb.QueryAsync(q, ct);
            var first = resp?.Results?.FirstOrDefault();
            if (first is null) return null;

            // Mapping "best effort"
            // Example: total number of traffic accidents => Approximate congestion level
            var accidentsTotal = TryInt(first, "nombre_d_accidents_de_la_circulation_total");
            var entite = TryString(first, "entite") ?? "ODWB";

            return new TrafficConditionDTO
            {
                Latitude = (decimal)latitude,
                Longitude = (decimal)longitude,
                DateCondition = DateTime.UtcNow,
                CongestionLevel = accidentsTotal is null ? "N/A" : (accidentsTotal.Value >= 20 ? "4" : "2"),
                IncidentType = $"ODWB accidents ({entite})"
            };
        }

        private static int? TryInt(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var i) ? i : null;

        private static string? TryString(Dictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) ? v?.ToString() : null;
    }

}
