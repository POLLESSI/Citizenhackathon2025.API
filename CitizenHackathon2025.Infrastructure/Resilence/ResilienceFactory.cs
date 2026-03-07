using CitizenHackathon2025.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;

namespace CitizenHackathon2025.Infrastructure.Resilience
{
    public static class ResilienceFactory
    {
        public static ResiliencePipelines Create(IServiceProvider sp)
        {
            var logger = sp.GetRequiredService<ILogger<ResiliencePipelines>>();
            return new ResiliencePipelines
            {
                OpenAi = CreatePipeline("OpenAI", logger),
                Traffic = CreatePipeline("Traffic", logger),
                Weather = CreatePipeline("Weather", logger),
                Ollama = CreatePipeline("Ollama", logger, TimeSpan.FromSeconds(300))
            };
        }

        private static AsyncPolicy<HttpResponseMessage> CreatePipeline(
            string name,
            ILogger logger,
            TimeSpan? timeout = null)
        {
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Retry {RetryCount} for {PolicyName}: Status={Status}. Delay={Delay}s",
                            retryCount,
                            name,
                            response.Exception?.Message ?? response.Result?.StatusCode.ToString(),
                            delay.TotalSeconds);
                    });

            AsyncPolicy<HttpResponseMessage> timeoutPolicy;
            if (timeout.HasValue)
            {
                timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeout.Value);
            }
            else
            {
                timeoutPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            }

            return Policy.WrapAsync(retryPolicy, timeoutPolicy);
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.