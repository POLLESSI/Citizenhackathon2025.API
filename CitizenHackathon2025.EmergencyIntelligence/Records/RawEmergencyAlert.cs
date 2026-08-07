namespace CitizenHackathon2025.EmergencyIntelligence.Records
{
    /// <summary>
    /// Raw representation of an alert as received from an external source.
    /// It has not yet been normalized into an EmergencyAlert.
    /// </summary>
    public sealed record RawEmergencyAlert(string SourceCode, string ExternalId, string RawPayload, string ContentType, DateTimeOffset ReceivedAtUtc, Uri? SourceUri = null, string? ETag = null, DateTimeOffset? LastModifiedUtc = null);
}








































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.