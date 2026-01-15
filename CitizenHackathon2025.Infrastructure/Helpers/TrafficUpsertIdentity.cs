using CitizenHackathon2025.Domain.Entities;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Helpers
{
    public static class TrafficUpsertIdentity
    {
        public static void Ensure(TrafficCondition tc, string defaultProvider)
        {
            if (tc is null) throw new ArgumentNullException(nameof(tc));

            if (string.IsNullOrWhiteSpace(tc.Provider))
                tc.Provider = defaultProvider;

            if (tc.LastSeenAt == default)
                tc.LastSeenAt = DateTime.UtcNow;

            // ExternalId required
            if (string.IsNullOrWhiteSpace(tc.ExternalId))
            {
                // Ultimate fallback if nothing is known: GUID (unique, not stable)
                tc.ExternalId = $"{tc.Provider}-{Guid.NewGuid():N}";
            }

            // Fingerprint = SHA-256 -> 32 bytes
            if (tc.Fingerprint is null || tc.Fingerprint.Length != 32)
            {
                var s = $"{tc.Provider}|{tc.ExternalId}";
                tc.Fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(s));
            }
        }

        // Generates a stable ExternalId from a "current" DTO
        public static (string ExternalId, byte[] Fingerprint) BuildStableId(
            string provider,
            decimal lat,
            decimal lon,
            DateTime dateUtc,
            string incidentType,
            string? location,
            string? congestionLevel,
            TimeSpan timeBucket)
        {
            // time bucket (e.g., 1 minute)
            var ticks = dateUtc.Ticks - (dateUtc.Ticks % timeBucket.Ticks);
            var bucketUtc = new DateTime(ticks, DateTimeKind.Utc);

            // normalize lat/lon like your database (DECIMAL(9,2)/(9,3))
            var latN = Math.Round(lat, 2).ToString("0.00", CultureInfo.InvariantCulture);
            var lonN = Math.Round(lon, 3).ToString("0.000", CultureInfo.InvariantCulture);

            var text =
                $"{provider}|{latN}|{lonN}|{incidentType?.Trim()}|{bucketUtc:O}|{location?.Trim()}|{congestionLevel?.Trim()}";

            var fp = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            var externalId = Convert.ToHexString(fp).ToLowerInvariant(); // 64 chars
            return (externalId, fp);
        }
    }

}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.