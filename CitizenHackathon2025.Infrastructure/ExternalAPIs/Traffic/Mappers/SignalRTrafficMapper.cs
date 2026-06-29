using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    public sealed class SignalRTrafficMapper : ITrafficProviderMapper<SignalRTrafficRaw>
    {
        private readonly ITrafficConditionNormalizer _normalizer;

        public SignalRTrafficMapper(ITrafficConditionNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        public TrafficCondition? TryMap(SignalRTrafficRaw raw, DateTime utcNow)
        {
            var tc = new TrafficCondition
            {
                Latitude = raw.Latitude,
                Longitude = raw.Longitude,
                DateCondition = raw.SentAtUtc ?? utcNow,
                Provider = "signalr",
                CongestionLevel = raw.CongestionLevel ?? raw.Severity?.ToString() ?? "1",
                IncidentType = raw.IncidentType ?? "RealtimeTrafficAlert",
                Title = raw.Message,
                Road = raw.Location,
                Severity = raw.Severity
            };

            _normalizer.Normalize(tc);
            TrafficMapperDefaults.EnsureIdentity(tc, "signalr");

            return tc;
        }
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.