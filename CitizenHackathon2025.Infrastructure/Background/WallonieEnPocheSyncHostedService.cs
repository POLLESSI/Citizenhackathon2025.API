using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class WallonieEnPocheSyncHostedService : SafePeriodicBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PipelineOptions _options;

        protected override string ServiceName => nameof(WallonieEnPocheSyncHostedService);

        protected override TimeSpan Period =>
            TimeSpan.FromSeconds(Math.Max(60, _options.WallonieEnPoche.PeriodSeconds));

        public WallonieEnPocheSyncHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<PipelineOptions> options,
            ILogger<WallonieEnPocheSyncHostedService> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        protected override async Task ExecuteIterationAsync(CancellationToken ct)
        {
            if (!_options.WallonieEnPoche.Enabled)
                return;

            using var scope = _scopeFactory.CreateScope();

            var sync = scope.ServiceProvider.GetRequiredService<IWallonieEnPocheSyncService>();

            var report = await sync.SyncAsync(ct);

            // Your service is already publishing PlaceCreated/PlaceUpdated/EventCreated/EventUpdated.
            // Here, you can only log in if necessary.
        }
    }
}


































































































