using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class CrowdAdvisoryService : ICrowdAdvisoryService
    {
        private readonly ICrowdCalendarRepository _repo;

        public CrowdAdvisoryService(ICrowdCalendarRepository repo) => _repo = repo;

        public async Task<IEnumerable<string>> GetAdvisoriesForTodayAsync(string regionCode, int? placeId = null, TimeZoneInfo? tz = null)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var entries = await _repo.GetByDateAsync(todayUtc, regionCode, placeId);

            tz ??= TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");

            return entries
                .OrderByDescending(e => e.ExpectedLevel)
                .Select(e => FormatMessage(e, tz));
        }

        public async Task<IEnumerable<(CrowdCalendarEntry, string)>> GetDueAdvisoriesAsync(DateTime nowUtc, TimeZoneInfo? tz = null)
        {
            tz ??= TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
            var entries = await _repo.GetDueAdvisoriesAsync(nowUtc);

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            return entries
                .Where(e =>
                {
                    var startLocal = e.StartLocalTime ?? new TimeSpan(6, 0, 0); // default 06:00
                    var lead = TimeSpan.FromHours(Math.Max(0, e.LeadHours));
                    var warnAt = startLocal - lead;

                    // Prevents values ​​< 00:00
                    var windowStart = warnAt < TimeSpan.Zero ? TimeSpan.Zero : warnAt;
                    var windowEnd = windowStart.Add(TimeSpan.FromMinutes(10));

                    var nowTod = nowLocal.TimeOfDay;
                    return nowTod >= windowStart && nowTod < windowEnd;
                })
                .Select(e => (e, FormatMessage(e, tz)));
        }

        private static string FormatMessage(CrowdCalendarEntry e, TimeZoneInfo tz)
        {
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            var startLocal = e.StartLocalTime ?? new TimeSpan(9, 0, 0);
            var recommendedArrival = DateTime.SpecifyKind(todayLocal.Add(startLocal).AddMinutes(-45), DateTimeKind.Unspecified);
            var recommendedDeparture = recommendedArrival.AddHours(-2); // simple heuristic
            var maxPrefix = e.ExpectedLevel switch
            {
                CrowdLevelEnum.Critical => "🚨 CRITIQUE",
                CrowdLevelEnum.High => "⚠️ IMPORTANT",
                CrowdLevelEnum.Medium => "ℹ️",
                _ => "✅"
            };

            var template = e.MessageTemplate ??
                "Attention, {Level} crowd expected today{EventSuffix}. Recommended departure time is {RecommendedDeparture}.";

            var txt = template
                .Replace("{Level}", e.ExpectedLevel.ToString())
                .Replace("{EventName}", e.EventName ?? "")
                .Replace("{EventSuffix}", string.IsNullOrWhiteSpace(e.EventName) ? "" : $" ({e.EventName})")
                .Replace("{RecommendedArrival}", recommendedArrival.ToString("HH:mm"))
                .Replace("{RecommendedDeparture}", recommendedDeparture.ToString("HH:mm"))
                .Replace("{Region}", e.RegionCode);

            return $"{maxPrefix} — {txt}";
        }
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.