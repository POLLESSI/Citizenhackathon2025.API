using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.Place;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IPlaceRepository
    {
        Task<IEnumerable<Place?>> GetLatestPlaceAsync();
        Task<Place> SavePlaceAsync(Place @place);
    }
}
