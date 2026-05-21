using CitizenHackathon2025.Infrastructure.ExternalProviders.Common;
using Polly;
using Polly.Extensions.Http;

namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common;

public static class HttpPolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
        ExternalProviderOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .WaitAndRetryAsync(
                options.RetryCount,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(
        ExternalProviderOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerFailures,
                TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds));
    }
}























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.