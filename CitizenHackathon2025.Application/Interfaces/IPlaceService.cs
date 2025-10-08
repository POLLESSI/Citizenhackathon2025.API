using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IPlaceService
    {
#nullable disable
        Task<List<Place>> GetLatestPlaceAsync(int limit = 200, CancellationToken ct = default);
        Task<Place?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Place> SaveAsync(Place place, CancellationToken ct = default);
        Place? UpdatePlace(Place @place);
        Task<PlaceDTO?> UpdateAsync(PlaceDTO dto);
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.