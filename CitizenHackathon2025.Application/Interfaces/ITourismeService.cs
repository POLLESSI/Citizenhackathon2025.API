using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.TrafficCondition;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ITourismeService
    {
        Task<string> GetSmartSuggestionsAsync(string userContext);
    }
}
