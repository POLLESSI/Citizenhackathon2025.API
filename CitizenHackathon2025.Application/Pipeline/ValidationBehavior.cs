using MediatR;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Application.Abstractions.Validations;

namespace CitizenHackathon2025.Application.Pipeline
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<ILocalValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(IEnumerable<ILocalValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            foreach (var validator in _validators)
            {
                _logger.LogDebug("Validating {RequestType} using {ValidatorType}", typeof(TRequest).Name, validator.GetType().Name);
                await validator.ValidateAsync(request, cancellationToken);
            }

            return await next();
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.