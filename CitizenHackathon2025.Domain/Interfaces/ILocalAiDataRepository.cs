using CitizenHackathon2025.Domain.DTOs;

namespace CitizenHackathon2025.Domain.Interfaces
{
    /// <summary>
    /// Provides a lightweight, query-oriented local AI data projection layer.
    /// The goal is not to expose rich domain entities, but only the minimal DTOs
    /// needed to build an accurate and bounded AI prompt context.
    /// </summary>
    public interface ILocalAiDataRepository
    {
        /// <summary>
        /// Returns nearby events relevant to the requested date and radius.
        /// Results should already be filtered geographically and temporally.
        /// </summary>
        Task<IEnumerable<LocalAiEventContextDTO>> GetNearbyEventsAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        /// <summary>
        /// Returns nearby crowd calendar items relevant to the requested date and radius.
        /// Intended for planned / forecast crowd-sensitive events.
        /// </summary>
        Task<IEnumerable<LocalAiCrowdCalendarContextDTO>> GetNearbyCrowdCalendarAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        /// <summary>
        /// Returns recent observed crowd information around the specified location.
        /// The implementation should interpret targetDate pragmatically, typically
        /// as "around that day" or "recent observations relevant to that day".
        /// </summary>
        Task<IEnumerable<LocalAiCrowdInfoContextDTO>> GetNearbyCrowdInfoAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        /// <summary>
        /// Returns nearby traffic incidents or traffic conditions with practical user impact.
        /// The implementation should filter by geographic radius and temporal relevance.
        /// </summary>
        Task<IEnumerable<LocalAiTrafficContextDTO>> GetNearbyTrafficAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);

        /// <summary>
        /// Returns nearby weather points / forecasts relevant to the requested date and radius.
        /// Only the fields needed for practical outing guidance should be returned.
        /// </summary>
        Task<IEnumerable<LocalAiWeatherContextDTO>> GetNearbyWeatherAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default);
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.