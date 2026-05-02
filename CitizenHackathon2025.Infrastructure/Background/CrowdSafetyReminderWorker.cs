using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class CrowdSafetyReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<CrowdHub> _hub;

        public CrowdSafetyReminderWorker(
            IServiceScopeFactory scopeFactory,
            IHubContext<CrowdHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();

                var repo = scope.ServiceProvider
                    .GetRequiredService<ICrowdSafetyAlertRepository>();

                var alerts = await repo.GetPendingRemindersAsync(50, stoppingToken);

                foreach (var alert in alerts)
                {
                    var dto = Map(alert);

                    await _hub.Clients
                        .Group(CrowdSafetyHubMethods.AuthorizedGroup)
                        .SendAsync(
                            CrowdSafetyHubMethods.ToClient.CrowdSafetyAlertReminder,
                            dto,
                            stoppingToken);

                    await repo.MarkReminderSentAsync(alert.Id, stoppingToken);
                }
            }
        }

        private static CrowdSafetyAlertDTO Map(CrowdSafetyAlert alert)
        {
            return new CrowdSafetyAlertDTO
            {
                Id = alert.Id,
                AntennaId = alert.AntennaId,
                EventId = alert.EventId,
                Severity = alert.Severity,
                Status = alert.Status,
                ActiveConnections = alert.ActiveConnections,
                UniqueDevices = alert.UniqueDevices,
                BaselineConnections = alert.BaselineConnections,
                IsRural = alert.IsRural,
                IsNight = alert.IsNight,
                IsKnownEvent = alert.IsKnownEvent,
                IsSensitiveZone = alert.IsSensitiveZone,
                Latitude = alert.Latitude,
                Longitude = alert.Longitude,
                Title = alert.Title,
                Message = alert.Message,
                DetectedAtUtc = alert.DetectedAtUtc,
                ValidatedAtUtc = alert.ValidatedAtUtc,
                ValidatedByUserId = alert.ValidatedByUserId,
                Active = alert.Active
            };
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.