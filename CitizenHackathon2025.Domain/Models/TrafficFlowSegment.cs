namespace CitizenHackathon2025.Domain.Models
{
    public sealed record TrafficFlowSegment(
        string Provider,
        string? ExternalId,
        byte[] Fingerprint,
        DateTime DateUtc,
        int? SpeedKmh,
        int? FreeFlowSpeedKmh,
        int? JamFactor,
        string? GeometryWkt // option simple sans geography
    );
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.