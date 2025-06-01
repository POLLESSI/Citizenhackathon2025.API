using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.Suggestion;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ISuggestionService
    {
#nullable disable
        Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync();
        Task<Suggestion> SaveSuggestionAsync(Suggestion @suggestion);
    }
}
