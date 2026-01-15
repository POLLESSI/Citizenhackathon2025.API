using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Mappers
{
    public static class OdwbTrafficMapper
    {
        public static TrafficCondition? TryMap(OdwbDynamicRecord r, DateTime utcNow)
        {
            // 1) ExternalId
            var externalId = GetString(r, "recordid")
                          ?? GetString(r, "id")
                          ?? GetString(r, "externalid")
                          ?? ComputeFingerprintText(r);

            // 2) Lat/Lon
            var lat = GetDecimal(r, "latitude") ?? GetDecimal(r, "lat");
            var lon = GetDecimal(r, "longitude") ?? GetDecimal(r, "lon");

            // Optional: geometry parsing if present (e.g. "geom", "geo_point_2d")
            // Keep simple; if lat/lon missing, skip.
            if (lat is null || lon is null) return null;

            // 3) Date
            var date = GetDateTimeUtc(r, "datecondition")
                    ?? GetDateTimeUtc(r, "date")
                    ?? GetDateTimeUtc(r, "datetime")
                    ?? utcNow;

            // 4) Type/level
            var incidentType = GetString(r, "incidenttype")
                            ?? GetString(r, "type")
                            ?? GetString(r, "event")
                            ?? "unknown";

            var congestion = GetString(r, "congestionlevel")
                          ?? GetString(r, "level")
                          ?? "unknown";

            // 5) Build entity
            return new TrafficCondition
            {
                Latitude = lat.Value,
                Longitude = lon.Value,
                DateCondition = date,
                CongestionLevel = congestion,
                IncidentType = incidentType,

                // ⚠️ only if your entity has these fields; otherwise ignore or extend entity
                Provider = "odwb",
                ExternalId = externalId,
                LastSeenAt = utcNow,
                Fingerprint = Sha256Bytes($"odwb|{externalId}")
            };
        }

        private static string? GetString(IDictionary<string, object?> r, string key)
            => r.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static decimal? GetDecimal(IDictionary<string, object?> r, string key)
        {
            if (!r.TryGetValue(key, out var v) || v is null) return null;
            if (v is decimal d) return d;
            if (v is double db) return (decimal)db;
            if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var x)) return x;
            if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.GetCultureInfo("fr-FR"), out x)) return x;
            return null;
        }

        private static DateTime? GetDateTimeUtc(IDictionary<string, object?> r, string key)
        {
            if (!r.TryGetValue(key, out var v) || v is null) return null;
            if (v is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            if (DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var x))
                return x.ToUniversalTime();
            return null;
        }

        private static string ComputeFingerprintText(IDictionary<string, object?> r)
        {
            // stable string for ExternalId fallback
            var sb = new StringBuilder();
            foreach (var kv in r.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
            return Convert.ToHexString(Sha256Bytes(sb.ToString())).ToLowerInvariant();
        }

        private static byte[] Sha256Bytes(string s) => SHA256.HashData(Encoding.UTF8.GetBytes(s));
    }

}
