using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Options;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CriticalAlertQuorumService : ICriticalAlertQuorumService
    {
        private readonly ICriticalAlertVoteRepository _repo;
        private readonly CriticalAlertRules _rule;
        private readonly ILogger<CriticalAlertQuorumService> _logger;

        public CriticalAlertQuorumService(ICriticalAlertVoteRepository repo, IOptions<CriticalAlertRules> options, ILogger<CriticalAlertQuorumService> logger)
        {
            _repo = repo;
            _rule = options.Value;
            _logger = logger;
        }

        public async Task<CriticalAlertQuorumResult>RegisterVoteAsync(CriticalAlertKind kind, int? placeId, decimal latitude, decimal longitude, string? deviceId, string? reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException(
                    "A device identifier is required " +
                    "for critical alert confirmation.",
                    nameof(deviceId));
            }

            var zoneKey =
                BuildZoneKey(
                    latitude,
                    longitude);

            var deviceHash =
                HashText(deviceId);

            await _repo.InsertAsync(
                new CriticalAlertVote
                {
                    AlertKind =
                        (byte)kind,

                    PlaceId =
                        placeId,

                    ZoneKey =
                        zoneKey,

                    DeviceHash =
                        deviceHash,

                    Latitude =
                        latitude,

                    Longitude =
                        longitude,

                    Reason =
                        reason
                },
                ct);

            var count =
                await _repo
                    .CountDistinctReportersAsync(
                        kind,
                        zoneKey,
                        _rule.WindowMinutes,
                        ct);

            var confirmed =
                count >=
                _rule.RequiredDistinctReports;

            _logger.LogInformation(
                "[CRITICAL QUORUM] " +
                "Kind={Kind}, " +
                "PlaceId={PlaceId}, " +
                "ZoneKey={ZoneKey}, " +
                "Device={DevicePrefix}, " +
                "Count={Count}/{Required}, " +
                "Confirmed={Confirmed}, " +
                "WindowMinutes={WindowMinutes}",
                kind,
                placeId,
                zoneKey,
                deviceHash[..Math.Min(
                    12,
                    deviceHash.Length)],
                count,
                _rule.RequiredDistinctReports,
                confirmed,
                _rule.WindowMinutes);

            return new CriticalAlertQuorumResult
            {
                Confirmed =
                    confirmed,

                ConfirmationCount =
                    count,

                RequiredCount =
                    _rule.RequiredDistinctReports,

                ZoneKey =
                    zoneKey
            };
        }

        private static string BuildZoneKey(decimal latitude, decimal longitude)
        {
            var latBucket =
                Math.Round(
                    latitude,
                    3,
                    MidpointRounding.AwayFromZero);

            var lngBucket =
                Math.Round(
                    longitude,
                    3,
                    MidpointRounding.AwayFromZero);

            return FormattableString.Invariant($"{latBucket:0.000}:{lngBucket:0.000}");
        }

        private static string HashText(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
            return Convert.ToHexString(bytes);
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.