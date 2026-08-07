namespace CitizenHackathon2025.EmergencyIntelligence.Records
{
    public sealed record EmergencyAlertCursor(string? ETag, DateTimeOffset? LastModifiedUtc, string? ContinuationToken, DateTimeOffset? LastSuccessfulFetchUtc)
    {
        public static EmergencyAlertCursor Empty { get; } =
            new(
                ETag: null,
                LastModifiedUtc: null,
                ContinuationToken: null,
                LastSuccessfulFetchUtc: null);
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.