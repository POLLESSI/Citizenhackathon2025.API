using CitizenHackathon2025.EmergencyIntelligence.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.EmergencyIntelligence.Workers
{
    public sealed class EmergencyAlertSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmergencyAlertSyncWorker> _logger;

        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public EmergencyAlertSyncWorker(IServiceScopeFactory scopeFactory, ILogger<EmergencyAlertSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmergencyAlertSyncWorker started.");

            using var timer = new PeriodicTimer(Interval);

            try
            {
                // Initial synchronization immediately upon startup.
                await SynchronizeAsync(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SynchronizeAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("EmergencyAlertSyncWorker stopped.");
            }
        }
        private async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Emergency alert synchronization beginning.");
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var orchestrator = scope.ServiceProvider.GetRequiredService<IEmergencyAlertSyncOrchestrator>();

                await orchestrator.SynchronizeAllAsync(cancellationToken);

                _logger.LogInformation("Emergency alert synchronization completed.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Emergency alert synchronization failed.");
            }
        }
    }
}















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.