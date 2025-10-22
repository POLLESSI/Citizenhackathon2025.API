using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Options;
using CitizenHackathon2025.Shared.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GptInteractionArchiverService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GptInteractionArchiverService> _logger;
        private readonly GptInteractionArchiverOptions _opt;
        private readonly object _lock = new();

        public GptInteractionArchiverService(IServiceScopeFactory scopeFactory, IOptionsMonitor<GptInteractionArchiverOptions> opt, ILogger<GptInteractionArchiverService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _opt = opt.Get("GptInteractions");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GptInteractionArchiverService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = DelayHelper.GetDelayUntilNextRun(_opt);
                try { await Task.Delay(delay, stoppingToken); } catch (TaskCanceledException) { break; }

                var entered = false;
                try
                {
                    Monitor.TryEnter(_lock, ref entered);
                    if (!entered) { _logger.LogWarning("GPT Interactions archiving overlap, skipping."); continue; }

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IGPTRepository>();
                    var n = await repo.ArchivePastGptInteractionsAsync();
                    _logger.LogInformation("Archived {Count} GPT interactions.", n);
                }
                catch (Exception ex) { _logger.LogError(ex, "GPT Interaction archiving error."); }
                finally { if (entered) Monitor.Exit(_lock); }
            }
            _logger.LogInformation("GptArchiverService stopped.");
        }
    }
}
