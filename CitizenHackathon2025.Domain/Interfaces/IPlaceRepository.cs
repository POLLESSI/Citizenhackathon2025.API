using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IPlaceRepository
    {
        Task<IEnumerable<Place?>> GetLatestPlaceAsync(int limit = 200, CancellationToken ct = default);
        Task<IEnumerable<Place>> GetNearbyPlacesAsync(double? latitude, double? longitude, int radiusKm, CancellationToken ct = default);

        Task<Place?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<Place> SavePlaceAsync(Place place, CancellationToken ct = default);
        Place? UpdatePlace(Place place);
        Task<Place?> UpdateAsync(Place place, CancellationToken ct = default);
    }

}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.