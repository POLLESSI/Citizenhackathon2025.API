namespace CitizenHackathon2025.Application.Time;

public static class DateTimeOffsetHelpers
{
    // Cache: avoids looping FindSystemTimeZoneById
    private static readonly Lazy<TimeZoneInfo> BrusselsTz = new(() =>
    {
        // Windows: "Romance Standard Time"
        // Linux: "Europe/Brussels"
        try { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels"); }
    });

    public static DateTimeOffset FromUtcDateTime(DateTime dtUtc)
        => new(DateTime.SpecifyKind(dtUtc, DateTimeKind.Utc));

    public static DateTimeOffset FromLocalBrussels(DateTime dtLocal)
    {
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dtLocal, DateTimeKind.Unspecified), BrusselsTz.Value);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    public static DateTimeOffset TruncateToSecond(DateTimeOffset dto)
    {
        var ticks = dto.UtcTicks - (dto.UtcTicks % TimeSpan.TicksPerSecond);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}






























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.