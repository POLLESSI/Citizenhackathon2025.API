using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class LocalAiContextService : ILocalAiContextService
    {
        private readonly ILocalAiDataRepository _repo;
        private readonly ILogger<LocalAiContextService> _logger;

        private static readonly LocalAiContextLimits Limits = new();

        public LocalAiContextService(
            ILocalAiDataRepository repo,
            ILogger<LocalAiContextService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<LocalAiContextDTO> BuildContextAsync(
            string prompt,
            double? latitude,
            double? longitude,
            CancellationToken ct = default)
        {
            var lat = latitude ?? 50.4114;
            var lng = longitude ?? 4.4445;

            var targetDate = ResolveTargetDate(prompt);
            var intent = ResolveIntent(prompt);

            _logger.LogInformation(
                "Building local AI context. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, TargetDate={TargetDate:yyyy-MM-dd}, Intent={@Intent}",
                prompt?.Length ?? 0,
                lat,
                lng,
                targetDate,
                intent);

            var eventsTask = intent.NeedEvents
                ? _repo.GetNearbyEventsAsync(lat, lng, targetDate, Limits.RadiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiEventContextDTO>());

            var crowdCalendarTask = intent.NeedCrowdCalendar
                ? _repo.GetNearbyCrowdCalendarAsync(lat, lng, targetDate, Limits.RadiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdCalendarContextDTO>());

            var crowdInfoTask = intent.NeedCrowdInfo
                ? _repo.GetNearbyCrowdInfoAsync(lat, lng, targetDate, Limits.RadiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiCrowdInfoContextDTO>());

            var trafficTask = intent.NeedTraffic
                ? _repo.GetNearbyTrafficAsync(lat, lng, targetDate, Limits.RadiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiTrafficContextDTO>());

            var weatherTask = intent.NeedWeather
                ? _repo.GetNearbyWeatherAsync(lat, lng, targetDate, Limits.RadiusKm, ct)
                : Task.FromResult(Enumerable.Empty<LocalAiWeatherContextDTO>());

            await Task.WhenAll(eventsTask, crowdCalendarTask, crowdInfoTask, trafficTask, weatherTask);

            var events = (await eventsTask)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.CrowdLevel ?? int.MinValue)
                .Take(Limits.MaxEvents)
                .ToList();

            var crowdCalendar = (await crowdCalendarTask)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.ExpectedLevel ?? int.MinValue)
                .ThenByDescending(x => x.Confidence ?? int.MinValue)
                .Take(Limits.MaxCrowdCalendar)
                .ToList();

            var crowdInfo = (await crowdInfoTask)
                .OrderByDescending(x => x.Timestamp ?? DateTime.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxCrowdInfo)
                .ToList();

            var traffic = (await trafficTask)
                .OrderByDescending(x => x.Severity ?? int.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxTraffic)
                .ToList();

            var weather = (await weatherTask)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.DateWeather ?? DateTime.MaxValue)
                .Take(Limits.MaxWeather)
                .ToList();

            return new LocalAiContextDTO
            {
                UserPrompt = prompt ?? string.Empty,
                Latitude = lat,
                Longitude = lng,
                TargetDate = targetDate,
                Events = events,
                CrowdCalendar = crowdCalendar,
                CrowdInfo = crowdInfo,
                Traffic = traffic,
                Weather = weather
            };
        }

        public string BuildPrompt(LocalAiContextDTO context)
        {
            var sb = new StringBuilder(2048);

            sb.AppendLine("You are OutZen local assistant.");
            sb.AppendLine("Use only the facts below.");
            sb.AppendLine("Do not invent events, traffic, weather, or attendance.");
            sb.AppendLine("If data is missing, say it clearly.");
            sb.AppendLine("Answer in French.");
            sb.AppendLine("Be concise, concrete, useful.");
            sb.AppendLine();

            sb.AppendLine($"Question: {context.UserPrompt}");
            sb.AppendLine($"Date: {context.TargetDate:yyyy-MM-dd}");
            sb.AppendLine($"Coordinates: {context.Latitude:F6}, {context.Longitude:F6}");
            sb.AppendLine();

            AppendEvents(sb, context);
            AppendCrowdCalendar(sb, context);
            AppendCrowdInfo(sb, context);
            AppendTraffic(sb, context);
            AppendWeather(sb, context);

            sb.AppendLine("Rules:");
            sb.AppendLine("- mention the most relevant nearby fact first");
            sb.AppendLine("- mention traffic/weather only if they impact the answer");
            sb.AppendLine("- do not mention anything outside the provided context");
            sb.AppendLine("- if nothing exact is available, mention the closest relevant items only");

            return sb.ToString();
        }

        private static void AppendEvents(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Nearby events:");
            if (context.Events.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var e in context.Events)
            {
                sb.AppendLine(
                    $"- {e.Title ?? "Unknown event"}, {e.City ?? "Unknown city"}, {(e.EventDate?.ToString("yyyy-MM-dd") ?? "—")}, " +
                    $"{FmtTs(e.StartTime)}-{FmtTs(e.EndTime)}, " +
                    $"crowd {(e.CrowdLevel?.ToString() ?? "—")}, " +
                    $"capacity {(e.MaxCapacity?.ToString() ?? "—")}, " +
                    $"{FmtDistance(e.DistanceKm)}, advice: {e.Advice ?? "—"}");
            }

            sb.AppendLine();
        }

        private static void AppendCrowdCalendar(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Planned crowd/events:");
            if (context.CrowdCalendar.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var e in context.CrowdCalendar)
            {
                sb.AppendLine(
                    $"- {e.EventName ?? "Unknown event"}, {e.RegionCode ?? "Unknown region"}, {(e.DateUtc?.ToString("yyyy-MM-dd") ?? "—")}, " +
                    $"{FmtTs(e.StartLocalTime)}-{FmtTs(e.EndLocalTime)}, " +
                    $"level {(e.ExpectedLevel?.ToString() ?? "—")}, " +
                    $"confidence {(e.Confidence?.ToString() ?? "—")}%, " +
                    $"{FmtDistance(e.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendCrowdInfo(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Recent observed crowd:");
            if (context.CrowdInfo.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var c in context.CrowdInfo)
            {
                sb.AppendLine(
                    $"- {c.LocationName ?? "Unknown place"}, " +
                    $"level {(c.CrowdLevel?.ToString() ?? "—")}, " +
                    $"{(c.Timestamp?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{FmtDistance(c.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendTraffic(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Traffic:");
            if (context.Traffic.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var t in context.Traffic)
            {
                var label = t.Title ?? t.IncidentType ?? t.Road ?? "Traffic incident";

                sb.AppendLine(
                    $"- {label}, " +
                    $"severity {(t.Severity?.ToString() ?? "—")}, " +
                    $"{(t.DateCondition?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{FmtDistance(t.DistanceKm)}");
            }

            sb.AppendLine();
        }

        private static void AppendWeather(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Weather:");
            if (context.Weather.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var w in context.Weather)
            {
                sb.AppendLine(
                    $"- {(w.DateWeather?.ToString("yyyy-MM-dd HH:mm") ?? "—")}, " +
                    $"{(w.TemperatureC?.ToString() ?? "—")}°C, " +
                    $"humidity {(w.Humidity?.ToString() ?? "—")}%, " +
                    $"wind {(w.WindSpeedKmh?.ToString("0.#") ?? "—")} km/h, " +
                    $"rain {(w.RainfallMm?.ToString("0.#") ?? "—")} mm, " +
                    $"severe {(w.IsSevere?.ToString() ?? "—")}");
            }

            sb.AppendLine();
        }

        private static string FmtTs(TimeSpan? ts)
            => ts.HasValue ? ts.Value.ToString(@"hh\:mm") : "—";

        private static string FmtDistance(double? distanceKm)
            => distanceKm.HasValue ? $"{distanceKm.Value:0.0} km" : "— km";

        private static LocalAiIntent ResolveIntent(string? prompt)
        {
            var p = (prompt ?? string.Empty).ToLowerInvariant();

            var asksTraffic =
                p.Contains("trafic") || p.Contains("traffic") || p.Contains("bouchon") ||
                p.Contains("route") || p.Contains("accident");

            var asksWeather =
                p.Contains("météo") || p.Contains("meteo") || p.Contains("pluie") ||
                p.Contains("vent") || p.Contains("orage") || p.Contains("température") || p.Contains("temperature");

            var asksCrowd =
                p.Contains("foule") || p.Contains("monde") || p.Contains("affluence") || p.Contains("crowd");

            var asksEvent =
                p.Contains("événement") || p.Contains("evenement") || p.Contains("activité") ||
                p.Contains("activite") || p.Contains("concert") || p.Contains("sortie") ||
                p.Contains("intéressant") || p.Contains("interessant") || p.Contains("quoi faire");

            if (!asksTraffic && !asksWeather && !asksCrowd && !asksEvent)
            {
                return new LocalAiIntent
                {
                    NeedEvents = true,
                    NeedCrowdCalendar = true,
                    NeedCrowdInfo = true,
                    NeedTraffic = true,
                    NeedWeather = true
                };
            }

            return new LocalAiIntent
            {
                NeedEvents = asksEvent,
                NeedCrowdCalendar = asksEvent || asksCrowd,
                NeedCrowdInfo = asksCrowd || asksEvent,
                NeedTraffic = asksTraffic,
                NeedWeather = asksWeather || asksEvent
            };
        }

        private static DateTime ResolveTargetDate(string? prompt)
        {
            var now = DateTime.Now;
            var p = prompt?.ToLowerInvariant() ?? string.Empty;

            if (p.Contains("ce weekend") || p.Contains("ce week-end"))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
                return now.Date.AddDays(daysUntilSaturday == 0 ? 0 : daysUntilSaturday);
            }

            if (p.Contains("demain"))
                return now.Date.AddDays(1);

            if (p.Contains("aujourd"))
                return now.Date;

            return now.Date;
        }
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.