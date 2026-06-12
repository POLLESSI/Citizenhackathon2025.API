using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Options;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CriticalAlertQuorumService : ICriticalAlertQuorumService
    {
        private readonly ICriticalAlertVoteRepository _repo;
        private readonly CriticalAlertRules _rules;

        public CriticalAlertQuorumService(
            ICriticalAlertVoteRepository repo,
            IOptions<CriticalAlertRules> options)
        {
            _repo = repo;
            _rules = options.Value;
        }

        public async Task<CriticalAlertQuorumResult> RegisterVoteAsync(
            CriticalAlertKind kind,
            int? placeId,
            decimal latitude,
            decimal longitude,
            string? deviceId,
            string? reason,
            CancellationToken ct = default)
        {
            var zoneKey = BuildZoneKey(latitude, longitude);

            await _repo.InsertAsync(new CriticalAlertVote
            {
                AlertKind = (byte)kind,
                PlaceId = placeId,
                ZoneKey = zoneKey,
                DeviceHash = string.IsNullOrWhiteSpace(deviceId) ? null : HashText(deviceId),
                Latitude = latitude,
                Longitude = longitude,
                Reason = reason
            }, ct);

            var count = await _repo.CountDistinctReportersAsync(
                kind,
                zoneKey,
                _rules.WindowMinutes,
                ct);

            return new CriticalAlertQuorumResult
            {
                Confirmed = count >= _rules.RequiredDistinctReports,
                ConfirmationCount = count,
                RequiredCount = _rules.RequiredDistinctReports,
                ZoneKey = zoneKey
            };
        }

        private static string BuildZoneKey(decimal latitude, decimal longitude)
        {
            var latBucket = Math.Round(latitude, 3);
            var lngBucket = Math.Round(longitude, 3);
            return $"{latBucket:0.000}:{lngBucket:0.000}";
        }

        private static string HashText(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
            return Convert.ToHexString(bytes);
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.