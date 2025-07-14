using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IPlaceService
    {
#nullable disable
        Task<IEnumerable<Place?>> GetLatestPlaceAsync();
        Task<Place?> GetPlaceByIdAsync(int id);
        Task<Place> SavePlaceAsync(Place @place);
        Place? UpdatePlace(Place @place);
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.