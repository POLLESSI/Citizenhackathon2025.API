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
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.