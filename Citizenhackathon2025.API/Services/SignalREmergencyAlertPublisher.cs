using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using CitizenHackathon2025.EmergencyIntelligence.Models;
using CitizenHackathon2025.Hubs.Services;

namespace CitizenHackathon2025.API.Services
{
    public sealed class SignalREmergencyAlertPublisher : IEmergencyAlertPublisher
    {
        private readonly EmergencyAlertHubBroadcaster _broadcaster;

        public SignalREmergencyAlertPublisher(EmergencyAlertHubBroadcaster broadcaster)
        {
            _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        }

        public Task PublishUpsertedAsync(EmergencyAlert alert, CancellationToken ct = default)
        {
            return _broadcaster.PublishUpsertedAsync(ToDto(alert), ct);
        }

        public Task PublishCancelledAsync(EmergencyAlert alert, CancellationToken ct = default)
        {
            return _broadcaster.PublishCancelledAsync(
                alert.Id,
                alert.SourceCode,
                alert.ExternalId,
                ct);
        }

        public Task PublishExpiredAsync(EmergencyAlert alert, CancellationToken ct = default)
        {
            return _broadcaster.PublishExpiredAsync(
                alert.Id,
                alert.SourceCode,
                alert.ExternalId,
                ct);
        }

        private static EmergencyAlertSignalRDTO ToDto(EmergencyAlert alert)
        {
            return new EmergencyAlertSignalRDTO
            {
                Id = alert.Id,
                SourceCode = alert.SourceCode,
                ExternalId = alert.ExternalId,
                HazardType = alert.HazardType,
                Severity = alert.Severity,
                Urgency = alert.Urgency,
                Certainty = alert.Certainty,
                Status = alert.Status,
                InformationKind = alert.InformationKind,
                Headline = alert.Headline,
                Description = alert.Description,
                Instructions = alert.Instructions,
                EffectiveFromUtc = alert.EffectiveFromUtc,
                LastUpdatedAtUtc = alert.LastUpdatedAtUtc,
                ProvinceCode = alert.ProvinceCode,
                MunicipalityCode = alert.MunicipalityCode,
                IsOfficial = alert.IsOfficial
            };
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.