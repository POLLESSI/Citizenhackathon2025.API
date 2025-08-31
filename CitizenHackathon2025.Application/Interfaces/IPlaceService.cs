using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
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