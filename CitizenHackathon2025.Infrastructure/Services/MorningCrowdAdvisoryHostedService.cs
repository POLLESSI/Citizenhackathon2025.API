using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Hubs.Hubs; // if you use IHubNotifier
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Periodically checks for any traffic warnings to push (SignalR/logs).
    /// </summary>
    public class MorningCrowdAdvisoryHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MorningCrowdAdvisoryHostedService> _logger;
        private readonly string _tzId;

        // Optional: if you have an AppOptions with TimeZoneId
        public class AdvisoryOptions
        {
            public string? TimeZoneId { get; set; } = "Europe/Brussels";
            public int PeriodSeconds { get; set; } = 30; // cadence
        }

        private readonly AdvisoryOptions _options;

        public MorningCrowdAdvisoryHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<MorningCrowdAdvisoryHostedService> logger,
            IOptions<AdvisoryOptions>? options = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options?.Value ?? new AdvisoryOptions();
            _tzId = string.IsNullOrWhiteSpace(_options.TimeZoneId) ? "Europe/Brussels" : _options.TimeZoneId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MorningCrowdAdvisoryHostedService started (period={Period}s, tz={Tz})",
                _options.PeriodSeconds, _tzId);

            // TZ resolution at runtime (less risky than in a field initializer)
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(_tzId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve time zone '{TzId}', falling back to UTC", _tzId);
                tz = TimeZoneInfo.Utc;
            }

            var period = TimeSpan.FromSeconds(Math.Max(5, _options.PeriodSeconds));
            using var timer = new PeriodicTimer(period);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var advisory = scope.ServiceProvider.GetRequiredService<ICrowdAdvisoryService>();
                    // Solve the notify only if you use it
                    var notifier = scope.ServiceProvider.GetService<IHubNotifier>();

                    var nowUtc = DateTime.UtcNow;
                    var due = await advisory.GetDueAdvisoriesAsync(nowUtc, tz);

                    if (due is null)
                        continue;

                    int count = 0;
                    foreach (var (entry, message) in due)
                    {
                        count++;
                        // Example: log + (optional) notify SignalR
                        _logger.LogDebug("Crowd advisory due: {Msg}", message);

                        if (notifier is not null)
                        {
                            // Adapt the target (group/connections) to your model
                            // await notifier.NotifyAsync(...);
                        }
                    }

                    if (count > 0)
                        _logger.LogInformation("Crowd advisories sent: {Count} (at {Now})", count, nowUtc);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Clean shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MorningCrowdAdvisoryHostedService loop");
                    // no rethrow: we wait for the next tick
                }
            }

            _logger.LogInformation("MorningCrowdAdvisoryHostedService stopped.");
        }
    }
}




































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.