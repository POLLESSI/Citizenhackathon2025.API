using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdCalendarRepository
    {
        Task<IEnumerable<CrowdCalendarEntry>> GetByDateAsync(DateTime dateUtc, string regionCode, int? placeId = null);
        Task<IEnumerable<CrowdCalendarEntry>> GetDueAdvisoriesAsync(DateTime nowUtc, string? regionFilter = null);
        Task<CrowdCalendarEntry?> GetByIdAsync(int id);
        Task<IEnumerable<CrowdCalendarEntry>> ListAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? region = null, int? placeId = null, bool? active = true);
        Task<int> InsertAsync(CrowdCalendarEntry e);
        Task<int> UpdateAsync(CrowdCalendarEntry e);
        Task<int> UpsertAsync(CrowdCalendarEntry e); // call MS
        Task<int> SoftDeleteAsync(int id);           // Active=0
        Task<int> RestoreAsync(int id);              // Active=1
        Task<int> HardDeleteAsync(int id);
        Task<IEnumerable<CrowdCalendarEntry>> GetDueTodayAsync(DateTime nowUtc, string? regionFilter = null);
    }
}
