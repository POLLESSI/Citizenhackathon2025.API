namespace CitizenHackathon2025.Application.Interfaces
{
    public interface ICrowdInfoAntennaConnectionService
    {
        // “Ping” = upsert LastSeen + FirstSeen so new
        Task PingAsync(
            int antennaId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            string? band,
            string? additionalJson,
            CancellationToken ct);
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.