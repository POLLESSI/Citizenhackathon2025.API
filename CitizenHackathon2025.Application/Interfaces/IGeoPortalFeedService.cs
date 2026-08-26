using CitizenHackathon2025.Contracts.DTOs.GeoPortal;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGeoPortalFeedService
    {
        Task<GeoPortalFeedSnapshotDto> GetAsync(
            CancellationToken cancellationToken = default);

        Task<GeoPortalFeedSnapshotDto> RefreshAsync(
            CancellationToken cancellationToken = default);
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.