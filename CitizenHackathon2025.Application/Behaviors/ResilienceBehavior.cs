using CitizenHackathon2025.Shared.Observability;
using CitizenHackathon2025.Shared.Resilience;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;

namespace CitizenHackathon2025.Application.Behaviors;

public class ResilienceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ResiliencePipeline<TResponse> _pipeline;
    private readonly ILogger<ResilienceBehavior<TRequest, TResponse>> _logger;

    public ResilienceBehavior(ILogger<ResilienceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _pipeline = ResilienceExec.BuildPipelineFor<TResponse>(_logger);
    }

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var (ctx, _) = ObservabilityContext.Create(
            service: "application",
            operation: typeof(TRequest).Name);

        return ResilienceExec.ExecuteLoggedAsync(
            _pipeline,
            _ => new ValueTask<TResponse>(next()),
            ctx,
            _logger,
            policyName: "cqrs",
            cancellationToken: ct);
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.