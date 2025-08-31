using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ISuggestionRepository
    {
        /// <summary>
        /// Retrieves active suggestions for a given user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Active Suggestion List</returns>
        Task<Suggestion?> GetByIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByUserAsync(int userId);

        /// <summary>
        /// Performs a logical deletion of a suggestion (Active = 0, DateDeleted = NOW).
        /// </summary>
        /// <param name="id">ID of the suggestion to disable</param>
        /// <returns>True if the operation was performed, false otherwise</returns>
        Task<bool> SoftDeleteSuggestionAsync(int id);
        Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync();
        Task<Suggestion> SaveSuggestionAsync(Suggestion @suggestion);
        Suggestion? UpdateSuggestion(Suggestion @suggestion);
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.