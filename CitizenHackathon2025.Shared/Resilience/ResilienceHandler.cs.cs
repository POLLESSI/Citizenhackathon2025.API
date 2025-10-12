using CitizenHackathon2025.Shared.Observability;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Shared.Resilience
{
    public sealed class ResilienceHandler : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
        private readonly ILogger<ResilienceHandler> _logger;

        public ResilienceHandler(
            ResiliencePipeline<HttpResponseMessage> pipeline,
            ILogger<ResilienceHandler> logger)
        {
            _pipeline = pipeline;
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var (ctx, _) = ObservabilityContext.Create(
                service: request.RequestUri?.Host ?? "unknown",
                operation: $"{request.Method} {request.RequestUri?.AbsolutePath}");

            // ExecuteLoggedAsync attend un Func<CancellationToken, ValueTask<T>>
            return Resilience.ExecuteLoggedAsync(
                _pipeline,
                token => new ValueTask<HttpResponseMessage>(base.SendAsync(request, token)),
                ctx,
                _logger,
                policyName: "http",
                cancellationToken: ct);
        }
    }
}