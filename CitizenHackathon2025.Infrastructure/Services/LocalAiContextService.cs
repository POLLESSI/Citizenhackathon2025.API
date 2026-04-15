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

            var safePrompt = prompt ?? string.Empty;
            var targetDate = ResolveTargetDate(safePrompt);
            var intent = ResolveIntent(safePrompt);

            _logger.LogInformation(
                "Building local AI context. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, TargetDate={TargetDate:yyyy-MM-dd}, Intent={@Intent}",
                safePrompt.Length,
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
                .Where(IsEventRelevant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.CrowdLevel ?? int.MinValue)
                .ThenBy(x => x.EventDate ?? DateTime.MaxValue)
                .Take(Limits.MaxEvents)
                .ToList();

            var crowdCalendar = (await crowdCalendarTask)
                .Where(IsCrowdCalendarRelevant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenByDescending(x => x.ExpectedLevel ?? int.MinValue)
                .ThenByDescending(x => x.Confidence ?? int.MinValue)
                .Take(Limits.MaxCrowdCalendar)
                .ToList();

            var crowdInfo = (await crowdInfoTask)
                .Where(IsCrowdInfoRelevant)
                .OrderByDescending(x => x.Timestamp ?? DateTime.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxCrowdInfo)
                .ToList();

            var traffic = (await trafficTask)
                .Where(IsTrafficRelevant)
                .OrderByDescending(x => x.Severity ?? int.MinValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .Take(Limits.MaxTraffic)
                .ToList();

            var weather = (await weatherTask)
                .Where(IsWeatherSignificant)
                .OrderBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.DateWeather ?? DateTime.MaxValue)
                .Take(Limits.MaxWeather)
                .ToList();

            _logger.LogInformation(
                "Local AI context built. Events={Events}, CrowdCalendar={CrowdCalendar}, CrowdInfo={CrowdInfo}, Traffic={Traffic}, Weather={Weather}",
                events.Count,
                crowdCalendar.Count,
                crowdInfo.Count,
                traffic.Count,
                weather.Count);

            return new LocalAiContextDTO
            {
                UserPrompt = safePrompt,
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
            ArgumentNullException.ThrowIfNull(context);

            var sb = new StringBuilder(4096);

            sb.AppendLine("You are OutZen local assistant.");
            sb.AppendLine("Answer only with facts present in the provided context.");
            sb.AppendLine("Do not invent places, events, traffic, weather, or crowd data.");
            sb.AppendLine("Answer in French.");
            sb.AppendLine("Be concise, concrete, and useful.");
            sb.AppendLine();

            sb.AppendLine("Priority rules:");
            sb.AppendLine("1. Prioritize real nearby places, visits, and events.");
            sb.AppendLine("2. Mention planned or observed crowd only if it helps the user choose where or when to go.");
            sb.AppendLine("3. Mention traffic only if it materially affects access or comfort.");
            sb.AppendLine("4. Mention weather only if it has a significant practical impact on the outing.");
            sb.AppendLine("5. Weather is never a point of interest by itself.");
            sb.AppendLine("6. Never present a raw weather observation as something interesting to see or do.");
            sb.AppendLine("7. If no real nearby place or event is available, say it clearly.");
            sb.AppendLine("8. If data is missing, say it clearly.");
            sb.AppendLine();

            sb.AppendLine("Response style:");
            sb.AppendLine("- start with the most relevant real nearby place or event if one exists");
            sb.AppendLine("- if none exists, explicitly say that no precise nearby place or event was found in the available data");
            sb.AppendLine("- add crowd, traffic, or weather only as practical context");
            sb.AppendLine("- do not turn temperature, rain, wind, or weather measurements into attractions");
            sb.AppendLine("- if there are several relevant nearby items, mention the closest or most useful ones first");
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

            sb.AppendLine("Final reminder:");
            sb.AppendLine("- real places and events first");
            sb.AppendLine("- practical constraints second");
            sb.AppendLine("- no invention");
            sb.AppendLine("- no weather-as-attraction");

            return sb.ToString();
        }

        private static bool IsEventRelevant(LocalAiEventContextDTO e)
        {
            if (e is null) return false;
            if (string.IsNullOrWhiteSpace(e.Title) && string.IsNullOrWhiteSpace(e.City)) return false;
            return true;
        }

        private static bool IsCrowdCalendarRelevant(LocalAiCrowdCalendarContextDTO e)
        {
            if (e is null) return false;
            if (string.IsNullOrWhiteSpace(e.EventName) && string.IsNullOrWhiteSpace(e.RegionCode)) return false;
            return true;
        }

        private static bool IsCrowdInfoRelevant(LocalAiCrowdInfoContextDTO c)
        {
            if (c is null) return false;
            if (string.IsNullOrWhiteSpace(c.LocationName)) return false;
            return true;
        }

        private static bool IsTrafficRelevant(LocalAiTrafficContextDTO t)
        {
            if (t is null) return false;

            return !string.IsNullOrWhiteSpace(t.Title)
                || !string.IsNullOrWhiteSpace(t.IncidentType)
                || !string.IsNullOrWhiteSpace(t.Road)
                || (t.Severity ?? 0) > 0;
        }

        private static bool IsWeatherSignificant(LocalAiWeatherContextDTO w)
        {
            if (w is null) return false;

            if (w.IsSevere == true) return true;
            if ((w.RainfallMm ?? 0d) >= 5.0d) return true;
            if ((w.WindSpeedKmh ?? 0d) >= 50.0d) return true;
            if ((w.TemperatureC ?? 15) <= 0) return true;
            if ((w.TemperatureC ?? 15) >= 32) return true;

            var main = (w.WeatherMain ?? string.Empty).ToLowerInvariant();
            var desc = (w.Description ?? string.Empty).ToLowerInvariant();
            var summary = (w.Summary ?? string.Empty).ToLowerInvariant();

            if (main.Contains("storm") || main.Contains("snow") || main.Contains("thunder"))
                return true;

            if (desc.Contains("orage") || desc.Contains("neige") || desc.Contains("forte pluie"))
                return true;

            if (summary.Contains("storm") || summary.Contains("snow") || summary.Contains("thunder"))
                return true;

            return false;
        }

        private static void AppendEvents(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Nearby real events and visits:");

            if (context.Events is null || context.Events.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var e in context.Events)
            {
                sb.AppendLine(
                    $"- {e.Title ?? "Unknown event"}, " +
                    $"{e.City ?? "Unknown city"}, " +
                    $"{(e.EventDate?.ToString("yyyy-MM-dd") ?? "—")}, " +
                    $"{FmtTs(e.StartTime)}-{FmtTs(e.EndTime)}, " +
                    $"crowd {(e.CrowdLevel?.ToString() ?? "—")}, " +
                    $"capacity {(e.MaxCapacity?.ToString() ?? "—")}, " +
                    $"{FmtDistance(e.DistanceKm)}, " +
                    $"advice: {e.Advice ?? "—"}");
            }

            sb.AppendLine();
        }

        private static void AppendCrowdCalendar(StringBuilder sb, LocalAiContextDTO context)
        {
            sb.AppendLine("Planned crowd-sensitive events:");

            if (context.CrowdCalendar is null || context.CrowdCalendar.Count == 0)
            {
                sb.AppendLine("- none");
                sb.AppendLine();
                return;
            }

            foreach (var e in context.CrowdCalendar)
            {
                sb.AppendLine(
                    $"- {e.EventName ?? "Unknown event"}, " +
                    $"{e.RegionCode ?? "Unknown region"}, " +
                    $"{(e.DateUtc?.ToString("yyyy-MM-dd") ?? "—")}, " +
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

            if (context.CrowdInfo is null || context.CrowdInfo.Count == 0)
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
            sb.AppendLine("Traffic with practical impact:");

            if (context.Traffic is null || context.Traffic.Count == 0)
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
            sb.AppendLine("Weather with practical impact:");

            if (context.Weather is null || context.Weather.Count == 0)
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
                    $"severe {(w.IsSevere?.ToString() ?? "—")}, " +
                    $"description: {w.Description ?? w.WeatherMain ?? w.Summary ?? "—"}");
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
                p.Contains("vent") || p.Contains("orage") || p.Contains("température") ||
                p.Contains("temperature");

            var asksCrowd =
                p.Contains("foule") || p.Contains("monde") || p.Contains("affluence") ||
                p.Contains("crowd");

            var asksEvent =
                p.Contains("événement") || p.Contains("evenement") ||
                p.Contains("activité") || p.Contains("activite") ||
                p.Contains("concert") || p.Contains("sortie") ||
                p.Contains("intéressant") || p.Contains("interessant") ||
                p.Contains("quoi faire") || p.Contains("à voir") || p.Contains("a voir");

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