using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.StaticConfig.Constants;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdSafetyDetectionService : ICrowdSafetyDetectionService
    {
        private readonly ICrowdInfoAntennaRepository _antennaRepo;
        private readonly ICrowdSafetyAlertRepository _alertRepo;

        public CrowdSafetyDetectionService(
            ICrowdInfoAntennaRepository antennaRepo,
            ICrowdSafetyAlertRepository alertRepo)
        {
            _antennaRepo = antennaRepo;
            _alertRepo = alertRepo;
        }

        public async Task EvaluateAntennaAsync(int antennaId, int activeConnections, int uniqueDevices, CancellationToken ct = default)
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

        private static bool IsSensitiveZone(double lat, double lng)
        {
            // Temporary version.
            // Later: SensitiveZone table with geography.
            return true;
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.