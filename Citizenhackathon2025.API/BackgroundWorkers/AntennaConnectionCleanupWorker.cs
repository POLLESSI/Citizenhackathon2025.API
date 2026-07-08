using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.API.BackgroundWorkers
{
    public sealed class AntennaConnectionCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AntennaConnectionCleanupWorker> _log;
        private readonly AntennaCleanupOptions _options;

        public AntennaConnectionCleanupWorker(IServiceScopeFactory scopeFactory, IOptions<AntennaCleanupOptions> options, ILogger<AntennaConnectionCleanupWorker> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _log.LogInformation("[AntennaCleanup] Disabled by configuration.");
                return;
            }

            var intervalSeconds = Math.Clamp(_options.IntervalSeconds, 5, 3600);

            _log.LogInformation(
                "[AntennaCleanup] Started. TimeoutSeconds={TimeoutSeconds}, IntervalSeconds={IntervalSeconds}, BatchSize={BatchSize}",
                _options.TimeoutSeconds,
                intervalSeconds,
                _options.BatchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var repo = scope.ServiceProvider
                        .GetRequiredService<ICrowdInfoAntennaConnectionRepository>();

                    var expiredConnections = await repo.ArchiveAndDeleteExpiredAsync(
                        _options.TimeoutSeconds,
                        _options.BatchSize,
                        stoppingToken);

                    var expiredAlerts = await repo.DeactivateAlertsWithoutActiveConnectionsAsync(
                        stoppingToken);

                    _log.LogInformation(
                        "[AntennaCleanup] Cleanup done. ExpiredConnections={ExpiredConnections}, ExpiredAlerts={ExpiredAlerts}",
                        expiredConnections,
                        expiredAlerts);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[AntennaCleanup] Cleanup failed.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(intervalSeconds),
                    stoppingToken);
            }
        }
    }
}













































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.