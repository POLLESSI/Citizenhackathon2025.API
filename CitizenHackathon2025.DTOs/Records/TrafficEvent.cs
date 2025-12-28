namespace CitizenHackathon2025.DTOs.Records
{
    public sealed record TrafficEvent(
        string Provider,
        string? ExternalId,
        byte[] Fingerprint,
        decimal Latitude,
        decimal Longitude,
        DateTime DateConditionUtc,
        string CongestionLevel,
        string IncidentType,
        string? Title,
        int? Severity
    );
}
