using CitizenHackathon2025.Shared.Observability;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// ✅ Explicit alias to the CLASS, not the namespace
using ResilienceExec = CitizenHackathon2025.Shared.Resilience.ResilienceExec;

namespace CitizenHackathon2025.Infrastructure.Resilience
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

            return ResilienceExec.ExecuteLoggedAsync(
                _pipeline,
                token => new ValueTask<HttpResponseMessage>(base.SendAsync(request, token)),
                ctx,
                _logger,
                policyName: "http",
                cancellationToken: ct);
        }
    }
}























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.