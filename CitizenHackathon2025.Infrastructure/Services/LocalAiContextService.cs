using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class LocalAiContextService : ILocalAiContextService
    {
        private readonly ILocalAiDataRepository _repo;
        private readonly ILogger<LocalAiContextService> _logger;

        public LocalAiContextService(
            ILocalAiDataRepository repo,
            ILogger<LocalAiContextService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<LocalAiContextDTO> BuildContextAsync(string prompt, double? latitude, double? longitude, CancellationToken ct = default)
        {
            var lat = latitude ?? 50.4114;
            var lng = longitude ?? 4.4445;

            var targetDate = ResolveTargetDate(prompt);
            var radiusKm = 25.0;

            var events = (await _repo.GetNearbyEventsAsync(lat, lng, targetDate, radiusKm, ct)).ToList();
            var crowdCalendar = (await _repo.GetNearbyCrowdCalendarAsync(lat, lng, targetDate, radiusKm, ct)).ToList();
            var crowdInfo = (await _repo.GetNearbyCrowdInfoAsync(lat, lng, targetDate, radiusKm, ct)).ToList();
            var traffic = (await _repo.GetNearbyTrafficAsync(lat, lng, targetDate, radiusKm, ct)).ToList();
            var weather = (await _repo.GetNearbyWeatherAsync(lat, lng, targetDate, radiusKm, ct)).ToList();

            return new LocalAiContextDTO
            {
                UserPrompt = prompt,
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
            var sb = new StringBuilder();

            sb.AppendLine("You are the local OutZen assistant.");
            sb.AppendLine("Answer only based on the data provided.");
            sb.AppendLine("Do not invent any events, incidents, or weather information.");
            sb.AppendLine("If information is not available, state it explicitly.");
            sb.AppendLine("Answer in French, clearly and usefully.");
            sb.AppendLine();

            sb.AppendLine($"User question : {context.UserPrompt}");
            sb.AppendLine($"Target date : {context.TargetDate:yyyy-MM-dd}");
            sb.AppendLine($"Reference coordinates : {context.Latitude:F6}, {context.Longitude:F6}");
            sb.AppendLine();

            sb.AppendLine("Planned events/crowds nearby :");
            if (context.CrowdCalendar.Count == 0)
            {
                sb.AppendLine("- No scheduled events found.");
            }
            else
            {
                foreach (var e in context.CrowdCalendar)
                {
                    sb.AppendLine(
                        $"- {e.EventName} to {e.RegionCode}, the {e.DateUtc:yyyy-MM-dd}, " +
                        $"of {e.StartLocalTime:hh\\:mm} to {e.EndLocalTime:hh\\:mm}, " +
                        $"planned level {e.ExpectedLevel}, trust {e.Confidence}%, " +
                        $"distance {e.DistanceKm:0.0} km, message : {e.MessageTemplate}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Crowd recently observed nearby :");
            if (context.CrowdInfo.Count == 0)
            {
                sb.AppendLine("- No recent crowd sightings found.");
            }
            else
            {
                foreach (var c in context.CrowdInfo)
                {
                    sb.AppendLine(
                        $"- {c.LocationName}, crowd level {c.CrowdLevel}, " +
                        $"observed the {c.Timestamp:yyyy-MM-dd HH:mm}, " +
                        $"distance {c.DistanceKm:0.0} km");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Traffic incidents nearby :");

            if (context.Traffic.Count == 0)
            {
                sb.AppendLine("- No significant traffic incidents were found.");
            }
            else
            {
                foreach (var t in context.Traffic)
                {
                    sb.AppendLine(
                        $"- {t.IncidentType}, " +
                        $"severity {t.Severity}, " +
                        $"observed the {t.DateCondition:yyyy-MM-dd HH:mm}, " +
                        $"distance {t.DistanceKm:0.0} km");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Near weather :");

            if (context.Weather.Count == 0)
            {
                sb.AppendLine("- No weather data found.");
            }
            else
            {
                foreach (var w in context.Weather)
                {
                    sb.AppendLine(
                        $"- {w.DateWeather:yyyy-MM-dd HH:mm} : {w.TemperatureC}°C, " +
                        $"humidity {w.Humidity}%, " +
                        $"wind {w.WindSpeedKmh} km/h, " +
                        $"rain {w.RainfallMm} mm, " +
                        $"weather severity = {w.IsSevere}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Final instruction :");
            sb.AppendLine("- If a major local event exists, mention it first.");
            sb.AppendLine("- Use CrowdInfo observations to qualify the actual attendance.");
            sb.AppendLine("- Mention traffic and weather only if they actually influence the outcome.");
            sb.AppendLine("- It does not refer to any event that is not in context.");
            sb.AppendLine("- If no exact local data exists, it suggests the elements closest to the context.");

            sb.AppendLine("Events nearby :");

            if (context.Events.Count == 0)
            {
                sb.AppendLine("- No events found.");
            }
            else
            {
                foreach (var e in context.Events)
                {
                    sb.AppendLine(
                        $"- {e.Title} to {e.City}, the {e.EventDate:yyyy-MM-dd}, " +
                        $"of {e.StartTime:hh\\:mm} to {e.EndTime:hh\\:mm}, " +
                        $"crowd level {e.CrowdLevel}, capacity {e.MaxCapacity}, " +
                        $"distance {e.DistanceKm:0.0} km, advice : {e.Advice}");
                }
            }
            return sb.ToString();
        }

        private static DateTime ResolveTargetDate(string prompt)
        {
            var now = DateTime.Now;
            var p = prompt?.ToLowerInvariant() ?? string.Empty;

            if (p.Contains("ce weekend") || p.Contains("ce week-end"))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
                if (daysUntilSaturday == 0) return now.Date;
                return now.Date.AddDays(daysUntilSaturday);
            }

            if (p.Contains("demain")) return now.Date.AddDays(1);
            if (p.Contains("aujourd")) return now.Date;

            return now.Date;
        }
    }
}
