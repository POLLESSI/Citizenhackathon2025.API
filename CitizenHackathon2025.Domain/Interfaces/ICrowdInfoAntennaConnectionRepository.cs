//using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ICrowdInfoAntennaConnectionRepository
    {
        Task UpsertPingAsync(
            int antennaId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            string? band,
            string? additionalJson,
            CancellationToken ct);

        Task<(int activeConnections, int uniqueDevices)> GetCountsAsync(
            int antennaId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken ct);
        Task<long> CreateAsync(
            int antennaId,
            int? eventId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            short? rssi,
            string? band,
            string? additionalJson,
            CancellationToken ct);

        Task<int> ArchiveAndDeleteExpiredAsync(int timeoutSeconds, int batchSize, CancellationToken ct);

        //Task<IReadOnlyList<DeletedAntennaConnectionDTO>> GetDeletedAsync(
        //    int antennaId,
        //    DateTime sinceUtc,
        //    int take,
        //    long? cursorDeletedId,
        //    CancellationToken ct);

        Task<int> PurgeDeletedArchiveAsync(int retentionDays, int batchSize, CancellationToken ct);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.