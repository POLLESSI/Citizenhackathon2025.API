using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    public sealed class ManualTrafficMapper : ITrafficProviderMapper<ManualTrafficRaw>
    {
        private readonly ITrafficConditionNormalizer _normalizer;

        public ManualTrafficMapper(ITrafficConditionNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public TrafficCondition? TryMap(ManualTrafficRaw raw, DateTime utcNow)
        {
            var tc = new TrafficCondition
            {
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                DateCondition = raw.DateUtc ?? utcNow,
                Provider = "manual",
                CongestionLevel = raw.CongestionLevel ?? raw.Severity?.ToString() ?? "1",
                IncidentType = raw.IncidentType ?? "ManualTrafficAlert",
                Title = raw.Message,
                Road = raw.Location,
                Severity = raw.Severity
            };

            _normalizer.Normalize(tc);
            TrafficMapperDefaults.EnsureIdentity(tc, "manual");

            return tc;
        }
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.