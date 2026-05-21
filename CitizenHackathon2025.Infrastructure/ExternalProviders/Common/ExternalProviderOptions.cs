namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common;

public sealed class ExternalProviderOptions
{
    public string BaseUrl { get; set; } = "";
    public string[] AllowedHosts { get; set; } = [];

    public bool RequireHttps { get; set; } = true;
    public bool AllowLoopback { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailures { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    public long MaxPayloadBytes { get; set; } = 1_048_576;
    public int RateLimitPerMinute { get; set; } = 60;
    public int CacheSeconds { get; set; } = 60;
}























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.