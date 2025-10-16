using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class MorningCrowdAdvisoryHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MorningCrowdAdvisoryHostedService> _logger;
        private readonly TimeZoneInfo _tz =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");

        public MorningCrowdAdvisoryHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<MorningCrowdAdvisoryHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Simple loop: checks every X seconds if an alert is due
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var advisory = scope.ServiceProvider.GetRequiredService<ICrowdAdvisoryService>();
                    var notifier = scope.ServiceProvider.GetRequiredService<IHubNotifier>(); // if you push messages

                    var nowUtc = DateTime.UtcNow;
                    var due = await advisory.GetDueAdvisoriesAsync(nowUtc, _tz);

                    foreach (var (entry, message) in due)
                    {
                        // to adapt: ​​send SignalR / log / email, etc.
                        _logger.LogInformation("Crowd advisory: {Msg}", message);
                        // await notifier.NotifyAsync(...);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MorningCrowdAdvisoryHostedService error");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
