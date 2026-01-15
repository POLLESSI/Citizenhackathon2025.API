using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherAlertsIngestionService : IWeatherAlertsIngestionService
    {
        private const string ProviderName = "openweather";

        private readonly IOpenWeatherAlertsClient _client;
        private readonly IWeatherAlertRepository _alertsRepo;
        private readonly ILogger<WeatherAlertsIngestionService> _log;

        public WeatherAlertsIngestionService(
            IOpenWeatherAlertsClient client,
            IWeatherAlertRepository alertsRepo,
            ILogger<WeatherAlertsIngestionService> log)
        {
            _client = client;
            _alertsRepo = alertsRepo;
            _log = log;
        }

        public async Task<(int AlertsUpserted, int ForecastSaved)> PullAndStoreAsync(
            decimal lat, decimal lon, CancellationToken ct = default)
        {
            OneCallResponse resp;
            try
            {
                resp = await _client.GetOneCallAsync(lat, lon, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OpenWeather OneCall failed lat={Lat} lon={Lon}", lat, lon);
                throw;
            }

            var alerts = resp.Alerts ?? new List<OneCallAlert>();
            if (alerts.Count == 0)
            {
                _log.LogInformation("No weather alerts lat={Lat} lon={Lon}", lat, lon);
                return (0, 0);
            }

            // NOTE: prefer a content-based id to avoid duplicates across pulls.
            var entities = alerts
                .Select(a => MapAlert(lat, lon, a))
                .GroupBy(a => a.ExternalId) // dedup this batch
                .Select(g => g.First())
                .ToList();

            var upserted = 0;

            foreach (var e in entities)
            {
                // If the contract is cancelled, stop immediately.
                ct.ThrowIfCancellationRequested();

                var saved = await _alertsRepo.UpsertAsync(e, ct);
                if (saved is not null) upserted++;
            }

            _log.LogInformation("Weather alerts upserted={Upserted} lat={Lat} lon={Lon}", upserted, lat, lon);

            return (upserted, 0);
        }

        private static WeatherAlertEntity MapAlert(decimal lat, decimal lon, OneCallAlert a)
        {
            var startUtc = DateTimeOffset.FromUnixTimeSeconds(a.Start).UtcDateTime;
            var endUtc = DateTimeOffset.FromUnixTimeSeconds(a.End).UtcDateTime;

            var sender = a.SenderName?.Trim();
            var evt = a.Event?.Trim();
            var desc = a.Description?.Trim();
            var tags = (a.Tags is null || a.Tags.Count == 0) ? null : string.Join(",", a.Tags);

            // ExternalId stable: must remain the same for the same alert
            // (Provider + lat/lon + sender + event + start/end + description)
            var externalId = ComputeSha256Hex($"{ProviderName}|{lat}|{lon}|{sender}|{evt}|{a.Start}|{a.End}|{desc}");

            return new WeatherAlertEntity
            {
                Provider = ProviderName,
                ExternalId = externalId,

                Latitude = lat,
                Longitude = lon,
                SenderName = sender,
                EventName = evt,
                StartUtc = startUtc,
                EndUtc = endUtc,
                Description = desc,
                Tags = tags,

                // Optional: If you don't have a score, leave it null.
                Severity = ComputeSeverity(evt, desc, a.Tags),
                // important: your MS is waiting @LastSeenAt
                LastSeenAt = DateTime.UtcNow,
                Active = true
            };
        }

        private static string ComputeSha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static byte? ComputeSeverity(string? evt, string? desc, List<string>? tags)
        {
            var text = $"{evt} {desc} {(tags is null ? "" : string.Join(' ', tags))}".ToLowerInvariant();

            // 4 - extreme
            if (ContainsAny(text, "tornado", "hurricane", "cyclone", "evacuat", "red alert", "extreme", "violent"))
                return 4;

            // 3 - severe
            if (ContainsAny(text, "thunderstorm", "storm", "gale", "flood", "inond", "heavy rain", "snowstorm", "blizzard", "ice", "verglas"))
                return 3;

            // 2 - moderate
            if (ContainsAny(text, "rain", "snow", "wind", "fog", "heat", "cold", "pluie", "neige", "vent", "brouillard", "canicule", "gel"))
                return 2;

            // 1 - low/advisory
            if (ContainsAny(text, "advisory", "watch", "warning", "alert", "vigilance"))
                return 1;

            return null;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.