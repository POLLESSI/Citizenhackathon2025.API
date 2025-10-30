using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Options;
using CitizenHackathon2025.Shared.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class TrafficConditionArchiverService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrafficConditionArchiverService> _logger;
        private readonly TrafficConditionArchiverOptions _opt;
        private readonly object _lock = new();

        public TrafficConditionArchiverService(IServiceScopeFactory scopeFactory, IOptionsMonitor<TrafficConditionArchiverOptions> opt, ILogger<TrafficConditionArchiverService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _opt = opt.Get("Traffic");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrafficArchiverService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = DelayHelper.GetDelayUntilNextRun(_opt);
                try { await Task.Delay(delay, stoppingToken); } catch (TaskCanceledException) { break; }

                var entered = false;
                try
                {
                    Monitor.TryEnter(_lock, ref entered);
                    if (!entered) { _logger.LogWarning("Traffic archiving overlap, skipping."); continue; }

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ITrafficConditionRepository>();
                    var n = await repo.ArchivePastTrafficConditionsAsync();
                    _logger.LogInformation("Archived {Count} traffic rows.", n);
                }
                catch (Exception ex) { _logger.LogError(ex, "Traffic archiving error."); }
                finally { if (entered) Monitor.Exit(_lock); }
            }
            _logger.LogInformation("TrafficArchiverService stopped.");
        }
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.