using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    public sealed class TomTomTrafficMapper : ITrafficProviderMapper<TomTomTrafficRaw>
    {
        private readonly ITrafficConditionNormalizer _normalizer;

        public TomTomTrafficMapper(ITrafficConditionNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public TrafficCondition? TryMap(TomTomTrafficRaw raw, DateTime utcNow)
        {
            var tc = new TrafficCondition
            {
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                DateCondition = raw.StartUtc ?? utcNow,
                Provider = "tomtom",
                CongestionLevel = raw.MagnitudeOfDelay?.ToString() ?? "1",
                IncidentType = raw.Category ?? "TomTomTrafficEvent",
                Title = raw.Description,
                Road = raw.RoadName,
                Severity = raw.MagnitudeOfDelay is >= 1 and <= 4 ? (byte?)raw.MagnitudeOfDelay : null
            };

            _normalizer.Normalize(tc);
            TrafficMapperDefaults.EnsureIdentity(tc, "tomtom");

            return tc;
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.