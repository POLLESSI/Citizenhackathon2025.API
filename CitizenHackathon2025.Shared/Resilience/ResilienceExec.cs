using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Prometheus;
using System.Diagnostics;

namespace CitizenHackathon2025.Shared.Resilience;

public static class ResilienceExec
{
    // Prometheus (if you want the same metrics on the CQRS side as well)
    private static readonly Counter Failures = Metrics.CreateCounter(
        "resilience_failures_total", "Failures",
        new CounterConfiguration { LabelNames = new[] { "policy", "service", "operation", "reason" } });

    private static readonly Histogram Duration = Metrics.CreateHistogram(
        "resilience_duration_seconds", "Exec duration",
        new HistogramConfiguration { LabelNames = new[] { "policy", "service", "operation" } });

    // Keys (same keys that your ObservabilityContext fills)
    private static readonly ResiliencePropertyKey<string> ServiceKey = new("service");
    private static readonly ResiliencePropertyKey<string> OperationKey = new("operation");
    private static readonly ResiliencePropertyKey<string> CorrelationIdKey = new("correlationId");

    public static async Task<T> ExecuteLoggedAsync<T>(
        ResiliencePipeline<T> pipeline,
        Func<CancellationToken, ValueTask<T>> action,
        ResilienceContext ctx,
        ILogger logger,
        string policyName,
        CancellationToken cancellationToken = default)
    {
        var service = ctx.Properties.GetValue(ServiceKey, "");
        var operation = ctx.Properties.GetValue(OperationKey, "");
        var corrId = ctx.Properties.GetValue(CorrelationIdKey, "");

        using var act = new Activity($"resilience:{service}/{operation}");
        act.AddTag("policy", policyName);
        act.AddTag("service", service);
        act.AddTag("operation", operation);
        act.AddTag("correlationId", corrId);
        act.Start();

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await pipeline.ExecuteAsync(_ => action(cancellationToken), ctx);
            Duration.WithLabels(policyName, service, operation).Observe(sw.Elapsed.TotalSeconds);
            return result;
        }
        catch (Exception ex)
        {
            Duration.WithLabels(policyName, service, operation).Observe(sw.Elapsed.TotalSeconds);
            Failures.WithLabels(policyName, service, operation, ex.GetType().Name).Inc();
            logger.LogError(ex, "Resilience failure {Policy} {Service}/{Operation} corr={CorrelationId}",
                policyName, service, operation, corrId);
            throw;
        }
        finally
        {
            act.Stop();
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    public static ResiliencePipeline<T> BuildPipelineFor<T>(ILogger logger) =>
        new ResiliencePipelineBuilder<T>()
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(15) })
            .AddRetry(new RetryStrategyOptions<T> { MaxRetryAttempts = 3 })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = _ => { logger.LogWarning("CQRS circuit opened"); return default; },
                OnClosed = _ => { logger.LogInformation("CQRS circuit closed"); return default; }
            })
            .Build();
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.