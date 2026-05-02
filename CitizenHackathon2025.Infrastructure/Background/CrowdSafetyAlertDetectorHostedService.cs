using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Models;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Background
{
    public sealed class CrowdSafetyAlertDetectorHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<CrowdSafetyHub> _hub;
        private readonly ILogger<CrowdSafetyAlertDetectorHostedService> _logger;

        public CrowdSafetyAlertDetectorHostedService(IServiceScopeFactory scopeFactory, IHubContext<CrowdSafetyHub> hub, ILogger<CrowdSafetyAlertDetectorHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DetectAsync(stoppingToken);
            }
        }

        private async Task DetectAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var antennaRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaRepository>();
                var connRepo = scope.ServiceProvider.GetRequiredService<ICrowdInfoAntennaConnectionRepository>();
                var alertRepo = scope.ServiceProvider.GetRequiredService<ICrowdSafetyAlertRepository>();
                var analyzer = scope.ServiceProvider.GetRequiredService<ICrowdSafetyAnalyzer>();

                var antennas = await antennaRepo.GetActiveAsync(ct);

                var nowUtc = DateTime.UtcNow;
                var windowStart = nowUtc.AddMinutes(-10);

                var belgiumTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, belgiumTimeZone);

                var isNight = nowLocal.Hour >= 22 || nowLocal.Hour <= 6;

                foreach (var antenna in antennas)
                {
                    var counts = await connRepo.GetCountsAsync(
                        antenna.Id,
                        windowStart,
                        nowUtc,
                        ct);

                    var baseline = await alertRepo.GetBaselineConnectionsAsync(
                        antenna.Id,
                        nowUtc,
                        ct);

                    var isRural = IsLikelyRural(antenna);

                    var ctx = new CrowdSafetyContext
                    {
                        ActiveConnections = counts.activeConnections,
                        UniqueDevices = counts.uniqueDevices,
                        MaxCapacity = antenna.MaxCapacity,
                        BaselineConnections = baseline,
                        IsNight = isNight,
                        IsRural = isRural,
                        IsKnownEvent = false,
                        IsSensitiveZone = false,
                        IsPersistent = false
                    };

                    var severity = analyzer.ComputeSeverity(ctx);

                    if (severity == 0)
                        continue;

                    var alreadyRaised = await alertRepo.HasRecentSimilarAlertAsync(
                        antenna.Id,
                        severity,
                        TimeSpan.FromMinutes(15),
                        ct);

                    if (alreadyRaised)
                        continue;

                    var alert = new CrowdSafetyAlert
                    {
                        AntennaId = antenna.Id,
                        Severity = severity,
                        Status = "PendingValidation",
                        ActiveConnections = counts.activeConnections,
                        UniqueDevices = counts.uniqueDevices,
                        BaselineConnections = baseline,
                        IsRural = isRural,
                        IsNight = isNight,
                        IsKnownEvent = false,
                        IsSensitiveZone = false,
                        Latitude = Math.Round((decimal)antenna.Latitude, 6),
                        Longitude = Math.Round((decimal)antenna.Longitude, 6),
                        Title = BuildTitle(severity),
                        Message = BuildMessage(severity, antenna.Name),
                        DetectedAtUtc = nowUtc,
                        Active = true
                    };

                    var saved = await alertRepo.InsertAsync(alert, ct);
                    var dto = MapToDto(saved);

                    await _hub.Clients
                        .Group(CrowdSafetyHubMethods.AuthorizedGroup)
                        .SendAsync(
                            CrowdSafetyHubMethods.ToClient.CrowdSafetyAlertRaised,
                            dto,
                            ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Crowd safety alert detection failed.");
            }
        }

        private static bool IsLikelyRural(CrowdInfoAntenna antenna)
        {
            var name = antenna.Name.ToLowerInvariant();
            var description = antenna.Description?.ToLowerInvariant() ?? "";

            var urbanHints = new[]
            {
                "bruxelles",
                "liège",
                "charleroi",
                "namur",
                "mons",
                "tournai",
                "verviers",
                "la louvière"
            };

            return !urbanHints.Any(x => name.Contains(x) || description.Contains(x));
        }

        private static string BuildTitle(byte severity)
            => severity switch
            {
                4 => "Concentration critique détectée",
                3 => "Concentration élevée détectée",
                2 => "Concentration inhabituelle détectée",
                1 => "Signal faible de concentration",
                _ => "Signal de foule"
            };

        private static string BuildMessage(byte severity, string antennaName)
            => severity switch
            {
                4 => $"Concentration anormale critique détectée près de {antennaName}. Validation humaine requise avant transmission.",
                3 => $"Concentration élevée détectée près de {antennaName}. Validation humaine recommandée.",
                2 => $"Concentration inhabituelle détectée près de {antennaName}. Surveillance recommandée.",
                _ => $"Signal faible de concentration détecté près de {antennaName}."
            };

        private static CrowdSafetyAlertDTO MapToDto(CrowdSafetyAlert alert)
            => new()
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













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.