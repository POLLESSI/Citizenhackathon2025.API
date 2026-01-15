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
                DateCondition = ev.DateConditionUtc,
                CongestionLevel = ev.CongestionLevel,
                IncidentType = ev.IncidentType,

                Provider = string.IsNullOrWhiteSpace(ev.Provider) ? "odwb" : ev.Provider,
                ExternalId = string.IsNullOrWhiteSpace(ev.ExternalId) ? $"auto-{Guid.NewGuid():N}" : ev.ExternalId!,
                Fingerprint = (ev.Fingerprint is { Length: 32 }) ? ev.Fingerprint : Hash32Fallback(ev),
                LastSeenAt = DateTime.UtcNow,

                Title = ev.Title,
                Severity = ev.Severity is null ? null : (byte?)Math.Clamp(ev.Severity.Value, 0, 255),
                // Road / GeomWkt si tu les as dans ton event provider
            };

        private static byte[] Hash32Fallback(TrafficEvent ev)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var s = $"{ev.Provider}|{ev.ExternalId}|{ev.Latitude}|{ev.Longitude}|{ev.DateConditionUtc:O}|{ev.IncidentType}";
            return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)); // 32 bytes
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.