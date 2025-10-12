using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Prometheus;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CitizenHackathon2025.Shared.Notifications;

namespace CitizenHackathon2025.Shared.Resilience
{
    public static class Resilience
    {
        // ---- Prometheus ----
        private static readonly Counter RetryCount = Metrics.CreateCounter(
            "polly_retries_total", "Retries by policy",
            new CounterConfiguration { LabelNames = new[] { "policy", "service", "operation" } });

        private static readonly Counter Failures = Metrics.CreateCounter(
            "polly_failures_total", "Failures by policy",
            new CounterConfiguration { LabelNames = new[] { "policy", "service", "operation", "reason" } });

        private static readonly Gauge CircuitState = Metrics.CreateGauge(
            "polly_circuit_state", "Circuit state (0 closed,1 open,0.5 half-open)",
            new GaugeConfiguration { LabelNames = new[] { "policy", "service" } });

        private static readonly Histogram Duration = Metrics.CreateHistogram(
            "polly_duration_seconds", "Exec duration",
            new HistogramConfiguration { LabelNames = new[] { "policy", "service", "operation" } });

        // ---- Keys for ResilienceContext.Properties ----
        private static readonly ResiliencePropertyKey<string> ServiceKey = new("service");
        private static readonly ResiliencePropertyKey<string> OperationKey = new("operation");
        private static readonly ResiliencePropertyKey<string> CorrelationIdKey = new("correlationId");

        public static ResiliencePipeline<HttpResponseMessage> BuildHttpPipeline(
            ILogger logger,
            INotifierAdmin notifier,      
            string policyName,
            int retry = 3,
            int breakerFailures = 5,
            int openSeconds = 30,
            int timeoutSeconds = 20)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                // (you can reset Timeout/Retry if you want)
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = breakerFailures,
                    BreakDuration = TimeSpan.FromSeconds(openSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<Exception>()
                        .HandleResult(r => !r.IsSuccessStatusCode),
                    OnOpened = async args =>
                    {
                        var (service, _) = Get(args.Context);
                        await notifier.NotifyAdminAsync(new
                        {
                            type = "circuit.opened",
                            policy = policyName,
                            service,
                            reason = args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString(),
                            openedAt = DateTime.UtcNow
                        });
                        logger.LogError("Circuit OPENED {Policy} {Service}", policyName, service);
                    },
                    OnClosed = args =>
                    {
                        var (service, _) = Get(args.Context);
                        logger.LogInformation("Circuit CLOSED {Policy} {Service}", policyName, service);
                        return default;
                    }
                })
                .Build();
        }


        public static async Task<T> ExecuteLoggedAsync<T>(
            ResiliencePipeline<T> pipeline,
            Func<CancellationToken, ValueTask<T>> action,
            ResilienceContext ctx,
            ILogger logger,
            string policyName,
            CancellationToken cancellationToken = default)
        {
            var (service, operation, corrId) = GetAll(ctx);

            using var act = new Activity($"polly:{service}/{operation}");
            act.AddTag("policy", policyName);
            act.AddTag("service", service);
            act.AddTag("operation", operation);
            act.AddTag("correlationId", corrId);
            act.Start();

            var sw = Stopwatch.StartNew();
            try
            {
                var result = await pipeline.ExecuteAsync(
                    (ResilienceContext _) => action(cancellationToken),
                    ctx);

                Duration.WithLabels(policyName, service, operation).Observe(sw.Elapsed.TotalSeconds);
                return result;
            }
            catch (Exception ex)
            {
                Duration.WithLabels(policyName, service, operation).Observe(sw.Elapsed.TotalSeconds);
                Failures.WithLabels(policyName, service, operation, ex.GetType().Name).Inc();
                logger.LogError(ex, "Polly failure {Policy} {Service}/{Operation} corr={CorrelationId}",
                    policyName, service, operation, corrId);
                throw;
            }
            finally
            {
                act.Stop();
                ResilienceContextPool.Shared.Return(ctx);
            }
        }
        public static ResiliencePipeline<T> BuildHttpPipelineFor<T>(ILogger logger) =>
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

        // --------- Helpers ----------
        private static (string service, string operation) Get(ResilienceContext ctx)
        {
            var service = ctx.Properties.GetValue(ServiceKey, "");
            var operation = ctx.Properties.GetValue(OperationKey, "");
            return (service, operation);
        }

        private static (string service, string operation, string corr) GetAll(ResilienceContext ctx)
        {
            var service = ctx.Properties.GetValue(ServiceKey, "");
            var operation = ctx.Properties.GetValue(OperationKey, "");
            var corr = ctx.Properties.GetValue(CorrelationIdKey, "");
            return (service, operation, corr);
        }
    }
}