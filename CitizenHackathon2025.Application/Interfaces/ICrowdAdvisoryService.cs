using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ICrowdAdvisoryService
    {
        Task<IEnumerable<string>> GetAdvisoriesForTodayAsync(string regionCode, int? placeId = null, TimeZoneInfo? tz = null);
        Task<IEnumerable<(CrowdCalendarEntry entry, string message)>> GetDueAdvisoriesAsync(DateTime nowUtc, TimeZoneInfo? tz = null);
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.