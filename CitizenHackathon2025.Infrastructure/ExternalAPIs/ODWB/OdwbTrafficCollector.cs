using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Services; // ITrafficIngestionService
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB
{
    public sealed class OdwbTrafficCollector : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OdwbTrafficCollector> _log;
        private readonly int _periodSeconds;
        private readonly int _limit;

        public OdwbTrafficCollector(
            IServiceScopeFactory scopeFactory,
            ILogger<OdwbTrafficCollector> log,
            IConfiguration cfg)
        {
            _scopeFactory = scopeFactory;
            _log = log;

            _periodSeconds = cfg.GetValue("Traffic:CollectorPeriodSeconds", 60);
            _limit = cfg.GetValue("ODWB:DefaultLimit", 20);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("ODWB collector started. period={Period}s limit={Limit}", _periodSeconds, _limit);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var ingestion = scope.ServiceProvider.GetRequiredService<ITrafficIngestionService>();

                    var q = new OdwbQuery(Limit: _limit);
                    var upserted = await ingestion.PullAndUpsertAsync(q, stoppingToken);

                    _log.LogInformation("ODWB collector tick upserted={Upserted}", upserted);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "ODWB collector tick failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(_periodSeconds), stoppingToken);
            }
        }
    }

}
