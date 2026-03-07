using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;

namespace CitizenHackathon2025.Infrastructure.Resilience
{
    public static class ResiliencePipelinesFactory
    {
        public static ResiliencePipelines Create(IServiceProvider sp)
        {
            var logger = sp.GetRequiredService<ILogger<ResiliencePipelines>>();
            return new ResiliencePipelines
            {
                OpenAi = CreatePipeline("OpenAI", logger, timeoutSeconds: 25),
                Traffic = CreatePipeline("Traffic", logger, timeoutSeconds: 10),
                Weather = CreatePipeline("Weather", logger, timeoutSeconds: 8),
                Ollama = CreatePipeline("Ollama", logger, timeoutSeconds: 300)
            };
        }

        private static AsyncPolicy<HttpResponseMessage> CreatePipeline(
            string name,
            ILogger logger,
            int retryCount = 3,
            int timeoutSeconds = 20)
        {
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        logger.LogWarning("Retry {RetryCount} for {PolicyName}: {StatusCode}. Delay: {Delay}s",
                            retryCount, name, response.Result?.StatusCode, delay.TotalSeconds);
                    });

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(timeoutSeconds));

            return Policy.WrapAsync(retryPolicy, timeoutPolicy);
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.