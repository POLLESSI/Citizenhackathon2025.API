using CitizenHackathon2025.API.Options;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.API.BackgroundWorkers
{
    public sealed class AntennaArchivePurgeWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AntennaArchivePurgeWorker> _log;
        private readonly AntennaArchiveRetentionOptions _opt;

        public AntennaArchivePurgeWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<AntennaArchiveRetentionOptions> options,
            ILogger<AntennaArchivePurgeWorker> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
            _opt = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromHours(Math.Clamp(_opt.IntervalHours, 1, 168));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaConnectionRepository>();

                    int totalPurged = 0;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var purged = await repo.PurgeDeletedArchiveAsync(
                            _opt.RetentionDays,
                            _opt.BatchSize,
                            stoppingToken);

                        totalPurged += purged;

                        // 0 => nothing left to purge
                        if (purged <= 0) break;
                    }

                    _log.LogInformation("Antenna deleted-archive purge done. Purged={Purged} RetentionDays={RetentionDays}",
                        totalPurged, _opt.RetentionDays);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Antenna deleted-archive purge failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}











































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.