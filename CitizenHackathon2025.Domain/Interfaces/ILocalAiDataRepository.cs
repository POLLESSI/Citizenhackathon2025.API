using CitizenHackathon2025.Domain.DTOs;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ILocalAiDataRepository
    {
        Task<IEnumerable<LocalAiEventContextDTO>> GetNearbyEventsAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        Task<IEnumerable<LocalAiCrowdCalendarContextDTO>> GetNearbyCrowdCalendarAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        Task<IEnumerable<LocalAiCrowdInfoContextDTO>> GetNearbyCrowdInfoAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        Task<IEnumerable<LocalAiTrafficContextDTO>> GetNearbyTrafficAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        Task<IEnumerable<LocalAiWeatherContextDTO>> GetNearbyWeatherAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.