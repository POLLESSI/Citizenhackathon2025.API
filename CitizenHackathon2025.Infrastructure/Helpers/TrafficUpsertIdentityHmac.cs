using CitizenHackathon2025.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Helpers
{
    public static class TrafficUpsertIdentityHmac
    {
        // timeBucket: ex. 1 minute pour stabiliser les événements "proches"
        public static void Ensure(
            TrafficCondition tc,
            string defaultProvider,
            byte[] hmacKey,
            TimeSpan? timeBucket = null)
        {
            if (tc is null) throw new ArgumentNullException(nameof(tc));
            if (hmacKey is null || hmacKey.Length < 16) // 16 min, 32+ recommandé
                throw new ArgumentException("HMAC key too short", nameof(hmacKey));

            tc.Provider = string.IsNullOrWhiteSpace(tc.Provider) ? defaultProvider : tc.Provider.Trim();
            tc.LastSeenAt = tc.LastSeenAt == default ? DateTime.UtcNow : tc.LastSeenAt;

            // Normalisation comme ta DB
            var latN = Math.Round(tc.Latitude, 2).ToString("0.00", CultureInfo.InvariantCulture);
            var lonN = Math.Round(tc.Longitude, 3).ToString("0.000", CultureInfo.InvariantCulture);

            var bucket = timeBucket ?? TimeSpan.FromMinutes(1);
            var dateUtc = tc.DateCondition.Kind == DateTimeKind.Utc
                ? tc.DateCondition
                : DateTime.SpecifyKind(tc.DateCondition, DateTimeKind.Utc);

            var bucketedUtc = Bucket(dateUtc, bucket);

            // ⚠️ Champ source pour stabiliser l'identité
            // Idée: provider + lat/lon normalisés + incidentType + bucket temps
            // + (optionnels) Title/Road/Severity si tu veux distinguer plus finement
            var incident = (tc.IncidentType ?? "").Trim();
            var congestion = (tc.CongestionLevel ?? "").Trim();

            var text = $"{tc.Provider}|{latN}|{lonN}|{incident}|{congestion}|{bucketedUtc:O}";

            // HMAC-SHA256 => 32 bytes
            var fp = HmacSha256(hmacKey, text);

            // ExternalId stable et compact : Base64Url(fp) ~ 43 chars
            tc.Fingerprint = fp;
            tc.ExternalId = WebEncoders.Base64UrlEncode(fp); // NVARCHAR(128) OK
        }

        private static DateTime Bucket(DateTime utc, TimeSpan bucket)
        {
            var ticks = utc.Ticks - (utc.Ticks % bucket.Ticks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static byte[] HmacSha256(byte[] key, string text)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }

}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.