using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IWallonieEnPocheSyncRepository
    {
        Task<UpsertPlaceResult> UpsertPlaceAsync(
            WepPlaceImportDTO dto,
            string externalSource,
            CancellationToken ct = default);

        Task<UpsertEventResult> UpsertEventAsync(
            WepEventImportDTO dto,
            string externalSource,
            CancellationToken ct = default);

        Task<int?> ResolvePlaceIdByExternalAsync(
            string externalSource,
            string externalId,
            CancellationToken ct = default);
    }

    public sealed class UpsertPlaceResult
    {
        public required Place Entity { get; init; }
        public bool Inserted { get; init; }
        public bool Updated { get; init; }
        public bool Skipped { get; init; }
    }

    public sealed class UpsertEventResult
    {
        public required Event Entity { get; init; }
        public bool Inserted { get; init; }
        public bool Updated { get; init; }
        public bool Skipped { get; init; }
    }
}





















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.