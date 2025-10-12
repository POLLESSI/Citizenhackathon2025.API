using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using Prometheus;

namespace CitizenHackathon2025.Shared.Resilience
{
    public static class PollyPoliciesV7
    {
        public static AsyncPolicyWrap<HttpResponseMessage> Build(string name, ILogger logger)
        {
            var retry = Policy<HttpResponseMessage>
                .Handle<Exception>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3,
                    attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                    onRetry: (outcome, delay, attempt, ctx) =>
                    {
                        logger.LogWarning("Retry {Attempt} {Policy} {Service}/{Operation} due to {Reason}",
                            attempt, name, ctx["service"], ctx["operation"],
                            outcome.Exception?.GetType().Name ?? outcome.Result.StatusCode.ToString());
                        Metrics.CreateCounter("polly_retries_total", "…", new CounterConfiguration { LabelNames = new[] { "policy", "service", "operation" } })
                               .WithLabels(name, (string)ctx["service"], (string)ctx["operation"]).Inc();
                    });

            var breaker = Policy<HttpResponseMessage>
                .Handle<Exception>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                    onBreak: (outcome, ts, ctx) =>
                    {
                        logger.LogError("Circuit OPEN {Policy} {Service}", name, ctx["service"]);
                        // Gauge state=1, SignalR admin push, AppInsights TrackEvent…
                    },
                    onReset: ctx => logger.LogInformation("Circuit CLOSED {Policy} {Service}", name, ctx["service"]),
                    onHalfOpen: () => { /* Gauge=0.5 */ });

            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(20, TimeoutStrategy.Optimistic, onTimeoutAsync: (ctx, ts, task, ex) =>
            {
                logger.LogWarning("Timeout {Policy} {Service}/{Operation} after {Sec}s", name, ctx["service"], ctx["operation"], ts.TotalSeconds);
                return Task.CompletedTask;
            });

            return Policy.WrapAsync(retry, breaker, timeout);
        }
    }
}
