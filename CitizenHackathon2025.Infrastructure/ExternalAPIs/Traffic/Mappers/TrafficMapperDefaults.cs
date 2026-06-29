using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.Helpers;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers
{
    internal static class TrafficMapperDefaults
    {
        public static void EnsureIdentity(
            TrafficCondition tc,
            string provider,
            TimeSpan? bucket = null)
        {
            var dateUtc = tc.DateCondition.Kind == DateTimeKind.Utc
                ? tc.DateCondition
                : tc.DateCondition.ToUniversalTime();

            var (externalId, fingerprint) = TrafficUpsertIdentity.BuildStableId(
                provider: provider,
                lat: tc.Latitude,
                lon: tc.Longitude,
                dateUtc: dateUtc,
                incidentType: tc.IncidentType,
                location: tc.Road,
                congestionLevel: tc.CongestionLevel,
                timeBucket: bucket ?? TimeSpan.FromMinutes(1));

            tc.Provider = provider;
            tc.ExternalId = externalId;
            tc.Fingerprint = fingerprint;
            tc.LastSeenAt = DateTime.UtcNow;
            tc.Active = true;
        }
    }
}















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.