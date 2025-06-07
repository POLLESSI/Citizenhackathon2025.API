using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Application.Services
{
    public class EventArchiverService : BackgroundService 
    {
        private readonly ILogger<EventArchiverService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public EventArchiverService(ILogger<EventArchiverService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("⏳ Starting automatic event archive check...");

                using (var scope = _scopeFactory.CreateScope())
                {
                    var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var archived = await eventRepo.ArchivePastEventsAsync();
                    _logger.LogInformation($"✅ Archived {archived} outdated event(s).");
                }

                // Wait 24 hours before the next execution
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
