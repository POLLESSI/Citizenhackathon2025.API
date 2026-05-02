using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Interfaces;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class TrafficConditionCollectorHostedService : SafePeriodicBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<TrafficHub> _hub;
        private readonly PipelineOptions _options;

        protected override string ServiceName => nameof(TrafficConditionCollectorHostedService);

        protected override TimeSpan Period =>
            TimeSpan.FromSeconds(Math.Max(10, _options.TrafficCondition.PeriodSeconds));

        public TrafficConditionCollectorHostedService(
            IServiceScopeFactory scopeFactory,
            IHubContext<TrafficHub> hub,
            IOptions<PipelineOptions> options,
            ILogger<TrafficConditionCollectorHostedService> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _options = options.Value;
        }

        protected override async Task ExecuteIterationAsync(CancellationToken ct)
        {
            if (!_options.TrafficCondition.Enabled)
                return;

            using var scope = _scopeFactory.CreateScope();

            var ingestion = scope.ServiceProvider.GetRequiredService<ITrafficOdwbIngestionService>();

            var upserted = await ingestion.SyncAsync(_options.TrafficCondition.Limit, ct);

            if (upserted > 0)
            {
                await _hub.Clients.All.SendAsync(
                    TrafficConditionHubMethods.ToClient.TrafficRefreshRequested,
                    "odwb-sync",
                    ct);
            }
        }
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.