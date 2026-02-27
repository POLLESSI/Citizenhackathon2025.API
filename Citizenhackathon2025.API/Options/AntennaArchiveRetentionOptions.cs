namespace CitizenHackathon2025.API.Options
{
    public sealed class AntennaArchiveRetentionOptions
    {
        public int RetentionDays { get; set; } = 30;   // X days
        public int BatchSize { get; set; } = 10_000;   // batch purge
        public int IntervalHours { get; set; } = 24;   // daily purge
    }
}
