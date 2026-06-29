namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Traffic.Mappers.Raws
{
    public sealed record PerexTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? Road,
        string? Title,
        string? IncidentType,
        string? CongestionLevel,
        byte? Severity,
        DateTime? DateUtc);

    public sealed record WazeTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? Street,
        string? City,
        string? Type,
        string? SubType,
        int? JamLevel,
        string? Description,
        DateTime? ReportedAtUtc);

    public sealed record HereTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? RoadName,
        string? EventCode,
        string? EventText,
        int? Criticality,
        DateTime? StartUtc);

    public sealed record TomTomTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? RoadName,
        string? Category,
        string? Description,
        int? MagnitudeOfDelay,
        DateTime? StartUtc);

    public sealed record ManualTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? Location,
        string? IncidentType,
        string? CongestionLevel,
        string? Message,
        byte? Severity,
        DateTime? DateUtc);

    public sealed record SignalRTrafficRaw(
        decimal Latitude,
        decimal Longitude,
        string? Location,
        string? IncidentType,
        string? CongestionLevel,
        string? Message,
        byte? Severity,
        string? DeviceId,
        DateTime? SentAtUtc);
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.