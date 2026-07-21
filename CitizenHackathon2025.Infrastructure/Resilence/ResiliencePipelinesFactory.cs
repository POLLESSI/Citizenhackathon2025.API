using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace CitizenHackathon2025.Infrastructure.Resilience
{
    public static class ResiliencePipelinesFactory
    {
        public static ResiliencePipelines Create(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ResiliencePipelines>>();

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var configuredValue = configuration.GetValue<int?>("MistralAI:GenerationTimeoutSeconds");

            var ollamaTimeoutSeconds = configuredValue.GetValueOrDefault(900);

            if (ollamaTimeoutSeconds < 60)
            {
                logger.LogError(
                    "[RESILIENCE CONFIG ERROR] Invalid Ollama timeout: {TimeoutSeconds}s. " +
                    "Forcing 900 seconds.",
                    ollamaTimeoutSeconds);

                ollamaTimeoutSeconds = 900;
            }

            logger.LogWarning(
                "[RESILIENCE ACTIVE] Ollama Timeout={TimeoutSeconds}s; " +
                "ConfigurationValue={ConfigurationValue}",
                ollamaTimeoutSeconds,
                configuredValue);

            return new ResiliencePipelines
            {
                OpenAi = CreateStandardPipeline("OpenAI", logger, TimeSpan.FromSeconds(25)),

                Traffic = CreateStandardPipeline("Traffic", logger, TimeSpan.FromSeconds(10)),

                Weather = CreateStandardPipeline("Weather", logger, TimeSpan.FromSeconds(8)),

                Ollama = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(ollamaTimeoutSeconds), TimeoutStrategy.Optimistic)
            };
        }

        private static AsyncPolicy<HttpResponseMessage>
            CreateStandardPipeline(string name, ILogger logger, TimeSpan timeout)
        {
            var retryPolicy = Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(response =>
                        response.StatusCode ==
                            System.Net.HttpStatusCode.RequestTimeout ||
                        response.StatusCode ==
                            System.Net.HttpStatusCode.TooManyRequests ||
                        (int)response.StatusCode >= 500)
                    .WaitAndRetryAsync(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, delay, attempt, context) =>
                        {
                            logger.LogWarning(
                                "[RESILIENCE] Retry {Attempt} for {Name}. " +
                                "Status={Status}; Error={Error}; Delay={Delay}s",
                                attempt,
                                name,
                                outcome.Result?.StatusCode,
                                outcome.Exception?.Message,
                                delay.TotalSeconds);
                        });

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeout, TimeoutStrategy.Optimistic);

            return Policy.WrapAsync(retryPolicy, timeoutPolicy);
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.