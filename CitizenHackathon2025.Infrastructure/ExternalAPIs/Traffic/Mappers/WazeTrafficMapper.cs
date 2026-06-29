using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    public sealed class WazeTrafficMapper : ITrafficProviderMapper<WazeTrafficRaw>
    {
        private readonly ITrafficConditionNormalizer _normalizer;

        public WazeTrafficMapper(ITrafficConditionNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public TrafficCondition? TryMap(WazeTrafficRaw raw, DateTime utcNow)
        {
            var road = string.Join(" - ",
                new[] { raw.Street, raw.City }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var tc = new TrafficCondition
            {
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                DateCondition = raw.ReportedAtUtc ?? utcNow,
                Provider = "waze",
                CongestionLevel = raw.JamLevel?.ToString() ?? "1",
                IncidentType = raw.SubType ?? raw.Type ?? "WazeAlert",
                Title = raw.Description,
                Road = string.IsNullOrWhiteSpace(road) ? null : road,
                Severity = raw.JamLevel is >= 1 and <= 4 ? (byte?)raw.JamLevel : null
            };

            _normalizer.Normalize(tc);
            TrafficMapperDefaults.EnsureIdentity(tc, "waze");

            return tc;
        }
    }
}





















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.