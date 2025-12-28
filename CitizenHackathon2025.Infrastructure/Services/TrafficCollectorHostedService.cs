using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Models;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Extensions;
using CitizenHackathon2025.Infrastructure.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class TrafficCollectorHostedService : BackgroundService
    {
        private readonly IEnumerable<ITrafficProvider> _providers;
        private readonly ITrafficConditionRepository _repo;
        private readonly IHubContext<TrafficHub> _hub;
        private readonly TrafficOptions _opt;
        private readonly ILogger<TrafficCollectorHostedService> _logger;

        public TrafficCollectorHostedService(
            IEnumerable<ITrafficProvider> providers,
            ITrafficConditionRepository repo,
            IHubContext<TrafficHub> hub,
            IOptions<TrafficOptions> options,
            ILogger<TrafficCollectorHostedService> logger)
        {
            _providers = providers;
            _repo = repo;
            _hub = hub;
            _opt = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var bbox = _opt.BBox.ToBoundingBox();

                    foreach (var p in _providers.Where(x => _opt.Providers.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
                    {
                        IReadOnlyList<TrafficEvent> incidents;

                        try
                        {
                            incidents = await p.GetIncidentsAsync(bbox, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Traffic provider {Provider} failed", p.Name);
                            continue;
                        }

                        foreach (var ev in incidents)
                        {
                            var entity = MapToEntity(ev);

                            var saved = await _repo.UpsertTrafficConditionAsync(entity);
                            if (saved is null) continue;

                            await _hub.Clients.All.SendAsync(
                                CitizenHackathon2025.Contracts.Hubs.TrafficConditionHubMethods.ToClient.TrafficUpdated,
                                saved.MapToTrafficConditionDTO(),
                                stoppingToken
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TrafficCollector loop failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(_opt.CollectorPeriodSeconds), stoppingToken);
            }
        }

        private static TrafficCondition MapToEntity(TrafficEvent ev)
            => new()
            {
                Latitude = ev.Latitude,
                Longitude = ev.Longitude,
                DateCondition = ev.DateConditionUtc,     // déjà UTC
                CongestionLevel = ev.CongestionLevel,
                IncidentType = ev.IncidentType
            };

    }
}