using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class CrowdInfoAntennaCollectorHostedService : SafePeriodicBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<CrowdHub> _hub;
        private readonly PipelineOptions _options;

        protected override string ServiceName => nameof(CrowdInfoAntennaCollectorHostedService);

        protected override TimeSpan Period =>
            TimeSpan.FromSeconds(Math.Max(5, _options.CrowdInfoAntenna.PeriodSeconds));

        public CrowdInfoAntennaCollectorHostedService(
            IServiceScopeFactory scopeFactory,
            IHubContext<CrowdHub> hub,
            IOptions<PipelineOptions> options,
            ILogger<CrowdInfoAntennaCollectorHostedService> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _options = options.Value;
        }

        protected override async Task ExecuteIterationAsync(CancellationToken ct)
        {
            if (!_options.CrowdInfoAntenna.Enabled)
                return;

            using var scope = _scopeFactory.CreateScope();

            var antennaRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaRepository>();
            var connectionRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaConnectionRepository>();
            var crowdRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoRepository>();

            var antennas = await antennaRepo.GetActiveAsync(ct);

            var nowUtc = DateTime.UtcNow;
            var windowStart = nowUtc.AddMinutes(-_options.CrowdInfoAntenna.WindowMinutes);

            foreach (var antenna in antennas)
            {
                var counts = await connectionRepo.GetCountsAsync(
                    antenna.Id,
                    windowStart,
                    nowUtc,
                    ct);

                var level = ComputeCrowdLevel(
                    activeConnections: counts.activeConnections,
                    maxCapacity: antenna.MaxCapacity);

                if (counts.activeConnections <= 0 || level < 2)
                    continue;

                var crowd = new CrowdInfo
                {
                    LocationName = antenna.Name,
                    Latitude = Math.Round((decimal)antenna.Latitude, 6),
                    Longitude = Math.Round((decimal)antenna.Longitude, 6),
                    CrowdLevel = level,
                    Timestamp = nowUtc,
                    Active = true
                };

                var saved = await crowdRepo.UpsertCrowdInfoAsync(crowd, ct);

                if (saved is not null)
                {
                    await _hub.Clients.All.SendAsync(
                        CrowdHubMethods.ToClient.ReceiveCrowdUpdate,
                        saved,
                        ct);
                }
            }

            await _hub.Clients.All.SendAsync(
                CrowdHubMethods.ToClient.CrowdRefreshRequested,
                "antenna-sync",
                ct);
        }

        private static int ComputeCrowdLevel(int activeConnections, int? maxCapacity)
        {
            if (maxCapacity is null or <= 0)
            {
                return activeConnections switch
                {
                    >= 200 => 4,
                    >= 100 => 3,
                    >= 40 => 2,
                    _ => 1
                };
            }

            var ratio = activeConnections / (double)maxCapacity.Value;

            return ratio switch
            {
                >= 0.90 => 4,
                >= 0.70 => 3,
                >= 0.40 => 2,
                _ => 1
            };
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.