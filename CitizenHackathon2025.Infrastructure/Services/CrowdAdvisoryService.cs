using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdAdvisoryService : ICrowdAdvisoryService
    {
        private static readonly TimeSpan DefaultStartLocalTime = new(9, 0, 0);
        private static readonly TimeSpan DefaultDueStartLocalTime = new(6, 0, 0);
        private static readonly TimeSpan AdvisoryWindowDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RecommendedArrivalOffset = TimeSpan.FromMinutes(45);
        private static readonly TimeSpan RecommendedDepartureOffset = TimeSpan.FromHours(2);

        private readonly ICrowdCalendarRepository _repository;

        public CrowdAdvisoryService(ICrowdCalendarRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<string>> GetAdvisoriesForTodayAsync(
            string regionCode,
            int? placeId = null,
            TimeZoneInfo? timeZone = null)
        {
            await _repository.ExpireOldEntriesAsync();

            var todayUtc = DateTime.UtcNow.Date;
            var entries = await _repository.GetByDateAsync(todayUtc, regionCode, placeId);
            var tz = ResolveTimeZone(timeZone);

            return entries
                .OrderByDescending(entry => entry.ExpectedLevel)
                .Select(entry => FormatMessage(entry, tz))
                .ToArray();
        }

        public async Task<IEnumerable<(CrowdCalendarEntry entry, string message)>> GetDueAdvisoriesAsync(
            DateTime nowUtc,
            TimeZoneInfo? timeZone = null)
        {
            await _repository.ExpireOldEntriesAsync();

            var tz = ResolveTimeZone(timeZone);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            var entries = await _repository.GetDueAdvisoriesAsync(nowUtc);

            return entries
                .Where(entry => IsDue(entry, nowLocal))
                .OrderByDescending(entry => entry.ExpectedLevel)
                .Select(entry => (entry, FormatMessage(entry, tz)))
                .ToArray();
        }

        private static TimeZoneInfo ResolveTimeZone(TimeZoneInfo? timeZone)
        {
            return timeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        }

        private static bool IsDue(CrowdCalendarEntry entry, DateTime nowLocal)
        {
            var startLocalTime = entry.StartLocalTime ?? DefaultDueStartLocalTime;
            var lead = TimeSpan.FromHours(Math.Max(0, entry.LeadHours));
            var warnAt = startLocalTime - lead;

            var windowStart = warnAt < TimeSpan.Zero
                ? TimeSpan.Zero
                : warnAt;

            var windowEnd = windowStart.Add(AdvisoryWindowDuration);
            var currentTimeOfDay = nowLocal.TimeOfDay;

            return currentTimeOfDay >= windowStart
                && currentTimeOfDay < windowEnd;
        }

        private static string FormatMessage(CrowdCalendarEntry entry, TimeZoneInfo timeZone)
        {
            var entryUtcDate = DateTime.SpecifyKind(entry.DateUtc.Date, DateTimeKind.Utc);
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(entryUtcDate, timeZone).Date;

            var startLocalTime = entry.StartLocalTime ?? DefaultStartLocalTime;
            var recommendedArrival = DateTime.SpecifyKind(
                localDate.Add(startLocalTime).Subtract(RecommendedArrivalOffset),
                DateTimeKind.Unspecified);

            var recommendedDeparture = recommendedArrival.Subtract(RecommendedDepartureOffset);

            var prefix = entry.ExpectedLevel switch
            {
                CrowdLevelEnum.Critical => "🚨 CRITIQUE",
                CrowdLevelEnum.High => "⚠️ IMPORTANT",
                CrowdLevelEnum.Medium => "ℹ️",
                _ => "✅"
            };

            var template = entry.MessageTemplate
                ?? "Attention, {Level} crowd expected today{EventSuffix}. Recommended departure time is {RecommendedDeparture}.";

            var message = template
                .Replace("{Level}", entry.ExpectedLevel.ToString())
                .Replace("{EventName}", entry.EventName ?? string.Empty)
                .Replace("{EventSuffix}", string.IsNullOrWhiteSpace(entry.EventName) ? string.Empty : $" ({entry.EventName})")
                .Replace("{RecommendedArrival}", recommendedArrival.ToString("HH:mm"))
                .Replace("{RecommendedDeparture}", recommendedDeparture.ToString("HH:mm"))
                .Replace("{Region}", entry.RegionCode);

            return $"{prefix} — {message}";
        }
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.