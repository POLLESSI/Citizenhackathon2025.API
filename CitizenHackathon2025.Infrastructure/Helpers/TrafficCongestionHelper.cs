namespace CitizenHackathon2025.Infrastructure.Helpers;

public static class TrafficCongestionHelper
{
    public static string NormalizeCongestionLevelQuery(string value)
    {
        var v = value.Trim().ToLowerInvariant();

        return v switch
        {
            "low" or "freeflow" or "free-flow" or "1" => "1",

            "medium" or "moderate" or "2" => "2",

            "high" or "heavy" or "3" => "3",

            "jammed" or "critical" or "blocked" or "4" => "4",

            _ => value.Trim()
        };
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.