using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdSafetyDetectionService : ICrowdSafetyDetectionService
    {
        private readonly ICrowdInfoAntennaRepository _antennaRepo;
        private readonly ICrowdSafetyAlertRepository _alertRepo;
        private readonly IMongoSnapshotWriter _mongoSnapshotWriter;
        private readonly ILogger<CrowdSafetyDetectionService> _logger;

        public CrowdSafetyDetectionService(ICrowdInfoAntennaRepository antennaRepo, ICrowdSafetyAlertRepository alertRepo, IMongoSnapshotWriter mongoSnapshotWriter, ILogger<CrowdSafetyDetectionService> logger)
        {
            _antennaRepo = antennaRepo;
            _alertRepo = alertRepo;
            _mongoSnapshotWriter = mongoSnapshotWriter;
            _logger = logger;
        }

        public async Task EvaluateAntennaAsync(int antennaId, int activeConnections, int uniqueDevices, CancellationToken ct = default)
        {
            

            try
            {
                var antenna = await _antennaRepo.GetByIdAsync(antennaId, ct);
                if (antenna is null) return;

                var capacity = antenna.MaxCapacity;

                var isCriticalByAbsoluteThreshold = activeConnections >= CrowdSafetyThresholds.CriticalAbsoluteConnections;

                var isCriticalByCapacity = capacity.HasValue && capacity.Value > 0 && ((double)activeConnections / capacity.Value) >= CrowdSafetyThresholds.CriticalCapacityRatio;

                var isCritical = isCriticalByAbsoluteThreshold || isCriticalByCapacity;

                var isUnexpected = true; // to be replaced by EventId null / no event scheduled
                var isSensitiveZone = IsSensitiveZone(antenna.Latitude, antenna.Longitude);

                if (!isCritical || !isUnexpected || !isSensitiveZone)
                    return;

                var alreadyExists = await _alertRepo.HasRecentSimilarAlertAsync(
                    antennaId,
                    minSeverity: 4,
                    cooldown: TimeSpan.FromMinutes(CrowdSafetyThresholds.AlertCooldownMinutes),
                    ct);

                if (alreadyExists)
                    return;

                await _alertRepo.InsertAsync(new CrowdSafetyAlert
                {
                    AntennaId = antennaId,
                    EventId = null,
                    Severity = CrowdSafetyThresholds.CriticalSeverity,
                    Status = "PendingValidation",
                    ActiveConnections = activeConnections,
                    UniqueDevices = uniqueDevices,
                    Latitude = (decimal)antenna.Latitude,
                    Longitude = (decimal)antenna.Longitude,
                    IsKnownEvent = false,
                    IsSensitiveZone = true,
                    IsRural = true,
                    IsNight = DateTime.UtcNow.Hour is >= 20 or < 6,
                    Title = "Concentration critique détectée",
                    Message = $"Concentration critique détectée près de {antenna.Name}. {activeConnections} connexions actives. Validation humaine requise.",
                    DetectedAtUtc = DateTime.UtcNow,
                    Active = true
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mongo snapshot write failed. SQL flow continues.");
            }
        }

        private static bool IsSensitiveZone(double lat, double lng)
        {
            // Temporary version.
            // Later: SensitiveZone table with geography.
            return true;
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.