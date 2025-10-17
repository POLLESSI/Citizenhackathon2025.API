using CitizenHackathon2025.Shared.Observability;
using CitizenHackathon2025.Shared.Resilience;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Application.Behaviors
{
    // ⚠️ Constraint required by MediatR v12
    public class ResilienceBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ResiliencePipeline<TResponse> _pipeline;
        private readonly ILogger<ResilienceBehavior<TRequest, TResponse>> _logger;

        // Simple variation: build the pipeline here (avoids open-generic injection)
        public ResilienceBehavior(ILogger<ResilienceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
            _pipeline = Resilience.BuildHttpPipelineFor<TResponse>(_logger); // see helper below
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var (ctx, _) = ObservabilityContext.Create(
                service: "application",
                operation: typeof(TRequest).Name,
                userId: Thread.CurrentPrincipal?.Identity?.Name);

            // ExecuteLoggedAsync expects Func<CancellationToken, ValueTask<TResponse>>
            return await Resilience.ExecuteLoggedAsync(
                _pipeline,
                _ => new ValueTask<TResponse>(next()),   // wrap Task -> ValueTask
                ctx,
                _logger,
                policyName: "cqrs");
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.