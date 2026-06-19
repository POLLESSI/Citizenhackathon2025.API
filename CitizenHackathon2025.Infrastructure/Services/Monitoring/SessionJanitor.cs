#nullable enable
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.DependencyInjection; // ✅ for CreateScope
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

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
            _log.LogInformation(
                "SessionJanitor started (interval: {Interval} min)",
                _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var repo = scope.ServiceProvider
                        .GetRequiredService<IUserSessionRepository>();

                    var deleted = await repo.PurgeExpiredAsync();

                    if (deleted > 0)
                    {
                        _log.LogInformation(
                            "SessionJanitor purged {Count} expired sessions.",
                            deleted);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "SessionJanitor purge failed");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
            }

            _log.LogInformation("SessionJanitor stopped.");
        }
    }
}

















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.