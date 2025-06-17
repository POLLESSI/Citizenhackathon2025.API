using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Application.Common.MediaR
{
    public abstract class HandlerBase<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger _logger;

        protected HandlerBase(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📨 Handling {RequestType}", typeof(TRequest).Name);
                var response = await HandleRequest(request, cancellationToken);
                _logger.LogInformation("✅ Handled {RequestType} successfully", typeof(TRequest).Name);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error handling {RequestType}", typeof(TRequest).Name);
                throw;
            }
        }
        protected abstract Task<TResponse> HandleRequest(TRequest request, CancellationToken cancellationToken);
    }
    
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.