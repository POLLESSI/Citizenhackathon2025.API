using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.EmergencyIntelligence.Models;

namespace CitizenHackathon2025.API.Services
{
    public static class EmergencyAlertDtoMapper
    {
        public static EmergencyAlertSignalRDTO ToSignalRDto(EmergencyAlert alert)
        {
            ArgumentNullException.ThrowIfNull(alert);

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