using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.API.BackgroundWorkers
{
    public sealed class WallonieEnPocheSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WallonieEnPocheSyncWorker> _logger;

        public WallonieEnPocheSyncWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<WallonieEnPocheSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IWallonieEnPocheSyncService>();

                    var report = await syncService.SyncAsync(stoppingToken);

                    _logger.LogInformation(
                        "WEP sync done. Places: +{PlacesInserted}/~{PlacesUpdated}, Events: +{EventsInserted}/~{EventsUpdated}, Errors={Errors}",
                        report.PlacesInserted,
                        report.PlacesUpdated,
                        report.EventsInserted,
                        report.EventsUpdated,
                        report.Errors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WallonieEnPoche sync worker failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}



















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.