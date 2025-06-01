using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.Suggestion;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface ISuggestionRepository
    {
        Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync();
        Task<Suggestion> SaveSuggestionAsync(Suggestion @suggestion);
    }
}
