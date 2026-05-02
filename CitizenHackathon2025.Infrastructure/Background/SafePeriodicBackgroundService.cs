using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public abstract class SafePeriodicBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        protected SafePeriodicBackgroundService(ILogger logger)
        {
            _logger = logger;
        }

        protected abstract string ServiceName { get; }
        protected abstract TimeSpan Period { get; }
        protected abstract Task ExecuteIterationAsync(CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ServiceName} started.", ServiceName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteIterationAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ServiceName} iteration failed.", ServiceName);
                }

                try
                {
                    await Task.Delay(Period, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("{ServiceName} stopped.", ServiceName);
        }
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.