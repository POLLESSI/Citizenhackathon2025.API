using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdInfoArchiverService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CrowdInfoArchiverService> _logger;
        private readonly CrowdInfoArchiverOptions _opt;
        private readonly object _runLock = new();

        public CrowdInfoArchiverService(IServiceScopeFactory scopeFactory, IOptionsMonitor<CrowdInfoArchiverOptions> opt, ILogger<CrowdInfoArchiverService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _opt = opt.Get("CrowdInfo"); 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CrowdInfoArchiverService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextRun(_opt);
                _logger.LogInformation("Next archive run in {Delay}.", delay);
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException) { break; }

                // Anti-overlap (in case the job lasts > 24h)
                var didEnter = false;
                try
                {
                    Monitor.TryEnter(_runLock, ref didEnter);
                    if (!didEnter)
                    {
                        _logger.LogWarning("Previous archiving still running. Skipping this cycle.");
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ICrowdInfoRepository>();

                    var affected = await repo.ArchivePastCrowdInfosAsync();
                    _logger.LogInformation("Archived {Count} CrowdInfo rows.", affected);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while archiving CrowdInfo.");
                }
                finally
                {
                    if (didEnter) Monitor.Exit(_runLock);
                }
            }

            _logger.LogInformation("CrowdInfoArchiverService stopped.");
        }

        private static TimeSpan GetDelayUntilNextRun(CrowdInfoArchiverOptions o)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(o.TimeZone);

            // now in TZ as DateTimeOffset
            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

            var nextLocal = new DateTimeOffset(
                nowLocal.Year, nowLocal.Month, nowLocal.Day,
                o.Hour, o.Minute, 0, nowLocal.Offset);

            if (nowLocal >= nextLocal)
                nextLocal = nextLocal.AddDays(1);

            // convert to UTC without TimeZoneInfo.ConvertTime 3-args (which does not exist for DateTimeOffset)
            var nextUtc = nextLocal.ToUniversalTime();

            return nextUtc - DateTimeOffset.UtcNow;
        }
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.