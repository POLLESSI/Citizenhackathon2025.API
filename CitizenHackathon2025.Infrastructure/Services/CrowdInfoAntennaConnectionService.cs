using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdInfoAntennaConnectionService : ICrowdInfoAntennaConnectionService
    {
        private readonly ICrowdInfoAntennaConnectionRepository _repo;

        public CrowdInfoAntennaConnectionService(ICrowdInfoAntennaConnectionRepository repo) => _repo = repo;

        public Task<IReadOnlyList<DeletedAntennaConnectionDTO>> GetDeletedAsync(int antennaId, DateTime sinceUtc, int take, long? cursorDeletedId, CancellationToken ct)
        {
            //    if (antennaId <= 0) throw new ArgumentOutOfRangeException(nameof(antennaId));
            //    if (take is < 1 or > 500) take = Math.Clamp(take, 1, 500);

            //    // sinceUtc doit être UTC
            //    if (sinceUtc.Kind == DateTimeKind.Unspecified)
            //        sinceUtc = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
            //    else if (sinceUtc.Kind == DateTimeKind.Local)
            //        sinceUtc = sinceUtc.ToUniversalTime();

            //    return _repo.GetDeletedAsync(antennaId, sinceUtc, take, cursorDeletedId, ct);
            throw new NotImplementedException();
        }
        public Task PingAsync(
            int antennaId,
            byte[] deviceHash,
            byte[]? ipHash,
            byte[]? macHash,
            byte source,
            short? signalStrength,
            string? band,
            string? additionalJson,
            CancellationToken ct)
        {
            if (deviceHash is null || deviceHash.Length != 32)
                throw new ArgumentException("deviceHash must be 32 bytes (BINARY(32)).", nameof(deviceHash));

            if (ipHash is not null && ipHash.Length != 32)
                throw new ArgumentException("ipHash must be 32 bytes (BINARY(32)).", nameof(ipHash));

            if (macHash is not null && macHash.Length != 32)
                throw new ArgumentException("macHash must be 32 bytes (BINARY(32)).", nameof(macHash));

            return _repo.UpsertPingAsync(antennaId, deviceHash, ipHash, macHash, source, signalStrength, band, additionalJson, ct);
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.