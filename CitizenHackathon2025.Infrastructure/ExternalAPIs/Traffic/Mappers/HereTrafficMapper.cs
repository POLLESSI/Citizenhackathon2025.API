using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    public sealed class HereTrafficMapper : ITrafficProviderMapper<HereTrafficRaw>
    {
        private readonly ITrafficConditionNormalizer _normalizer;

        public HereTrafficMapper(ITrafficConditionNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public TrafficCondition? TryMap(HereTrafficRaw raw, DateTime utcNow)
        {
            var tc = new TrafficCondition
            {
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                DateCondition = raw.StartUtc ?? utcNow,
                Provider = "here",
                CongestionLevel = raw.Criticality?.ToString() ?? "1",
                IncidentType = raw.EventCode ?? "HereTrafficEvent",
                Title = raw.EventText,
                Road = raw.RoadName,
                Severity = raw.Criticality is >= 1 and <= 4 ? (byte?)raw.Criticality : null
            };

            _normalizer.Normalize(tc);
            TrafficMapperDefaults.EnsureIdentity(tc, "here");

            return tc;
        }
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.