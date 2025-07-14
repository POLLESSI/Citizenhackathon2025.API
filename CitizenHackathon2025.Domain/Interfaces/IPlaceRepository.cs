using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IPlaceRepository
    {
        Task<IEnumerable<Place?>> GetLatestPlaceAsync();
        Task<Place?> GetPlaceByIdAsync(int id);
        Task<Place> SavePlaceAsync(Place @place);
        Place? UpdatePlace(Place @place);
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.