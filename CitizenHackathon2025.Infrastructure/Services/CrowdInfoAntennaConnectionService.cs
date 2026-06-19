using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdInfoAntennaConnectionService : ICrowdInfoAntennaConnectionService
    {
        private readonly ICrowdInfoAntennaConnectionRepository _repo;
        private readonly IMongoSnapshotWriter _mongoSnapshotWriter;
        private readonly ILogger<CrowdInfoAntennaConnectionService> _logger;

        public CrowdInfoAntennaConnectionService(ICrowdInfoAntennaConnectionRepository repo, IMongoSnapshotWriter mongoSnapshotWriter, ILogger<CrowdInfoAntennaConnectionService> logger)
        {
            _repo = repo;
            _mongoSnapshotWriter = mongoSnapshotWriter;
            _logger = logger;
        }

        public Task<IReadOnlyList<DeletedAntennaConnectionDTO>> GetDeletedAsync(int antennaId, DateTime sinceUtc, int take, long? cursorDeletedId, CancellationToken ct)
        {
            //    if (antennaId <= 0) throw new ArgumentOutOfRangeException(nameof(antennaId));
            //    if (take is < 1 or > 500) take = Math.Clamp(take, 1, 500);

            //    // since UTC must be UTC
            //    if (sinceUtc.Kind == DateTimeKind.Unspecified)
            //        sinceUtc = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
            //    else if (sinceUtc.Kind == DateTimeKind.Local)
            //        sinceUtc = sinceUtc.ToUniversalTime();

            //    return _repo.GetDeletedAsync(antennaId, sinceUtc, take, cursorDeletedId, ct);
            throw new NotImplementedException();
        }
        public async Task PingAsync(int antennaId, int? eventId, byte[] deviceHash, byte[]? ipHash, byte[]? macHash, byte source, short? signalStrength, string? band, string? additionalJson, CancellationToken ct)
        {
            if (antennaId <= 0)
                throw new ArgumentOutOfRangeException(nameof(antennaId));

            if (deviceHash is null || deviceHash.Length != 32)
                throw new ArgumentException("deviceHash must be 32 bytes.", nameof(deviceHash));

            if (ipHash is not null && ipHash.Length != 32)
                throw new ArgumentException("ipHash must be 32 bytes.", nameof(ipHash));

            if (macHash is not null && macHash.Length != 32)
                throw new ArgumentException("macHash must be 32 bytes.", nameof(macHash));

            await _repo.UpsertPingAsync(antennaId, eventId, deviceHash, ipHash, macHash, source, signalStrength, band, additionalJson, ct);
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.