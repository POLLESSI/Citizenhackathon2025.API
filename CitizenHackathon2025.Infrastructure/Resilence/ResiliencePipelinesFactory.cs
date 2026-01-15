using CitizenHackathon2025.Shared.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;

namespace CitizenHackathon2025.Infrastructure.Resilience;

public static class ResiliencePipelinesFactory
{
    public static ResiliencePipelines Create(IServiceProvider sp)
    {
        var lf = sp.GetRequiredService<ILoggerFactory>();
        var logger = lf.CreateLogger("Polly");
        var notifier = sp.GetRequiredService<INotifierAdmin>();

        return new ResiliencePipelines
        {
            OpenAi = CitizenHackathon2025.Infrastructure.Resilience.Resilience.BuildHttpPipeline(
                        logger, notifier, "openai", retry: 3, breakerFailures: 5, openSeconds: 30, timeoutSeconds: 25),

            Weather = CitizenHackathon2025.Infrastructure.Resilience.Resilience.BuildHttpPipeline(
                        logger, notifier, "openweather", retry: 2, breakerFailures: 4, openSeconds: 20, timeoutSeconds: 8),

            Traffic = CitizenHackathon2025.Infrastructure.Resilience.Resilience.BuildHttpPipeline(
                        logger, notifier, "trafficapi", retry: 3, breakerFailures: 5, openSeconds: 30, timeoutSeconds: 10),
        };
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.