using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Options;
using CitizenHackathon2025.Shared.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherForecastArchiverService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WeatherForecastArchiverService> _logger;
        private readonly WeatherForecastArchiverOptions _opt;
        private readonly object _lock = new();

        public WeatherForecastArchiverService(IServiceScopeFactory scopeFactory, IOptionsMonitor<WeatherForecastArchiverOptions> opt, ILogger<WeatherForecastArchiverService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _opt = opt.Get("Weather");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeatherArchiverService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = DelayHelper.GetDelayUntilNextRun(_opt);
                try { await Task.Delay(delay, stoppingToken); } catch (TaskCanceledException) { break; }

                var entered = false;
                try
                {
                    Monitor.TryEnter(_lock, ref entered);
                    if (!entered) { _logger.LogWarning("Weather archiving overlap, skipping."); continue; }

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IWeatherForecastRepository>();
                    var n = await repo.ArchivePastWeatherForecastsAsync();
                    _logger.LogInformation("Archived {Count} weather rows.", n);
                }
                catch (Exception ex) { _logger.LogError(ex, "Weather archiving error."); }
                finally { if (entered) Monitor.Exit(_lock); }
            }
            _logger.LogInformation("WeatherArchiverService stopped.");
        }
    }
}
