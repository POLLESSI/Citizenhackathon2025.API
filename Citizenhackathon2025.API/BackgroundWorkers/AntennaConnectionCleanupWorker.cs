using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.API.Options;

namespace CitizenHackathon2025.API.BackgroundWorkers
{
    public sealed class AntennaConnectionCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AntennaConnectionCleanupWorker> _log;
        private readonly AntennaCleanupOptions _options;

        public AntennaConnectionCleanupWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<AntennaCleanupOptions> options,
            ILogger<AntennaConnectionCleanupWorker> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider
                        .GetRequiredService<ICrowdInfoAntennaConnectionRepository>();

                    await repo.ArchiveAndDeleteExpiredAsync(
                        _options.TimeoutSeconds,
                        _options.BatchSize,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Antenna cleanup failed.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.IntervalSeconds),
                    stoppingToken);
            }
        }
    }
}













































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.