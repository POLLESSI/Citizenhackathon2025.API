namespace CitizenHackathon2025.Infrastructure.Options
{
    public sealed class GeoPortalFeedOptions
    {
        public const string SectionName = "GeoPortalFeed";

        public int CacheMinutes { get; init; } = 10;

        public int PartialFailureCacheMinutes { get; init; } = 2;

        public int StaleHours { get; init; } = 6;

        public int MaxFeedBytes { get; init; } = 1_048_576;

        public List<GeoPortalFeedSourceOptions> Sources { get; init; } = new();
    }


    public sealed class GeoPortalFeedSourceOptions
    {
        public string Code { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Kind { get; init; } = string.Empty;

        public string Url { get; init; } = string.Empty;
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.