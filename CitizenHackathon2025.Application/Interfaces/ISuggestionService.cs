﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ISuggestionService
    {
#nullable disable
        /// <summary>
        /// Gets all active suggestions from a given user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Active Suggestion List</returns>
        Task<Suggestion?> GetByIdAsync(int id);
        Task<IEnumerable<Suggestion>> GetSuggestionsByUserAsync(int userId);

        /// <summary>
        /// Logically deletes a suggestion via Active = 0 et DateDeleted = DateTime.Now.
        /// </summary>
        /// <param name="id">Suggestion ID</param>
        /// <returns>True if logical deletion was performed</returns>
        Task<bool> SoftDeleteSuggestionAsync(int id);
        Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync();
        Task<Suggestion> SaveSuggestionAsync(Suggestion @suggestion);
        Suggestion? UpdateSuggestion(Suggestion @suggestion);
    }
}












































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.