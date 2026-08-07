using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Enums.CitizenHackathon2025.Contracts.Enums;
using NetTopologySuite.Geometries;

namespace CitizenHackathon2025.EmergencyIntelligence.Models
{
    public sealed class EmergencyAlert
    {
        public Guid Id { get; set; }

        public required string SourceCode { get; set; }

        public required string ExternalId { get; set; }

        public string? ExternalReferenceId { get; set; }

        public string? CorrelationKey { get; set; }

        public EmergencyHazardType HazardType { get; set; }

        public EmergencySeverity Severity { get; set; }

        public EmergencyUrgency Urgency { get; set; }

        public EmergencyCertainty Certainty { get; set; }

        public EmergencyAlertStatus Status { get; set; }

        public SafetyInformationKind InformationKind { get; set; }

        public required string Headline { get; set; }

        public required string Description { get; set; }

        public string? Instructions { get; set; }

        public string Language { get; set; } = "fr-BE";

        public DateTimeOffset SentAtUtc { get; set; }

        public DateTimeOffset EffectiveFromUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public DateTimeOffset LastUpdatedAtUtc { get; set; }

        /// <summary>
        /// Point, Polygon or MultiPolygon in SRID 4326.
        /// </summary>
        public Geometry? Area { get; set; }

        /// <summary>
        /// Used when the source provides a point accompanied by a radius.
        /// </summary>
        public double? RadiusMeters { get; set; }

        public string? ProvinceCode { get; set; }

        public string? MunicipalityCode { get; set; }

        public Uri? OfficialInformationUri { get; set; }

        public bool IsOfficial { get; set; }

        public bool IsMachineVerified { get; set; }

        public required string PayloadHash { get; set; }

        public string? RawPayloadStorageKey { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.