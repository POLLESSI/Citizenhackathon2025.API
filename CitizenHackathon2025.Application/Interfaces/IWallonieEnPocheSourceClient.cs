using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    /// <summary>
    /// External source: Wallonia in Your Pocket (API / SQL / JSON / Fake)
    /// </summary>
    public interface IWallonieEnPocheSourceClient
    {
        /// <summary>
        /// Retrieve locations from the external source
        /// </summary>
        Task<IReadOnlyList<WepPlaceImportDTO>> GetPlacesAsync(CancellationToken ct = default);

        /// <summary>
        /// Retrieves events from the external source
        /// </summary>
        Task<IReadOnlyList<WepEventImportDTO>> GetEventsAsync(CancellationToken ct = default);
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.