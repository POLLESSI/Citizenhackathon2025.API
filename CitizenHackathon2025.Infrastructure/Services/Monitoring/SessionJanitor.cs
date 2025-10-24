#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // ✅ for CreateScope
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services.Monitoring
{
    public class SessionJanitor : BackgroundService
    {
        private readonly ILogger<SessionJanitor> _log;
        private readonly IServiceScopeFactory _scopeFactory;   // ✅ instead of injecting the repo
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public SessionJanitor(ILogger<SessionJanitor> log, IServiceScopeFactory scopeFactory)
        {
            _log = log;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("SessionJanitor started (interval: {Interval} min)", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope(); // ✅ create a scope
                    var repo = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>(); // ✅ scoped

                    var deleted = await repo.PurgeExpiredAsync();
                    if (deleted > 0)
                        _log.LogInformation("SessionJanitor purged {Count} expired sessions.", deleted);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "SessionJanitor purge failed");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { /* shutdown */ }
            }

            _log.LogInformation("SessionJanitor stopped.");
        }
    }
}