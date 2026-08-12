using CitizenHackathon2025.Application.Intelligence.AlertFusion;
using CitizenHackathon2025.Application.Intelligence.Decision;
using CitizenHackathon2025.Application.Intelligence.RiskAssessment;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Application.Intelligence.CommandCenter
{
    public sealed class CommandCenterService : ICommandCenterService
    {
        private readonly ICrowdSafetyAlertRepository _alertRepository;
        private readonly IAlertFusionEngine _fusion;
        private readonly IRiskScoreCalculator _risk;
        private readonly OfficialEmergencyRiskContextService _officialEmergencyRiskContext;
        private readonly IDecisionEngine _decisionEngine;

        public CommandCenterService(ICrowdSafetyAlertRepository alertRepository, IAlertFusionEngine fusion, IRiskScoreCalculator risk, OfficialEmergencyRiskContextService officialEmergencyRiskContext, IDecisionEngine decisionEngine)
        {
            _alertRepository = alertRepository;
            _fusion = fusion;
            _risk = risk;
            _officialEmergencyRiskContext = officialEmergencyRiskContext;
            _decisionEngine = decisionEngine;
        }
        public async Task<CommandCenterSnapshotDTO> GetSnapshotAsync(CancellationToken ct = default)
        {
            var clusters = await GetActiveIncidentsAsync(ct);

            return new CommandCenterSnapshotDTO
            {
                GeneratedAtUtc = DateTime.UtcNow,
                GlobalRiskScore = clusters.Count == 0 ? 0 : Math.Clamp((int)clusters.Average(c => c.RiskScore), 0, 100),
                CriticalIncidentCount = clusters.Count(c => c.Severity >= 4),
                HighIncidentCount = clusters.Count(c => c.Severity == 3),
                ModerateIncidentCount = clusters.Count(c => c.Severity == 2),
                TotalActiveConnections = clusters.Sum(c => c.TotalActiveConnections),
                Summary = clusters.Count == 0
                    ? "No active operational incident detected in Wallonia."
                    : $"{clusters.Count} active incident cluster(s) detected in Wallonia."
            };
        }


        public async Task<List<CrowdAlertCluster>> GetActiveIncidentsAsync(CancellationToken ct = default)
        {
            var alerts = await _alertRepository.GetLatestAsync(200, ct);
            var dtos = alerts
                .Where(a => a.Active)
                .Select(a =>
                    new CrowdSafetyAlertDTO
                    {
                        Id = a.Id,
                        AntennaId = a.AntennaId,
                        EventId = a.EventId,
                        Severity = a.Severity,
                        Status = a.Status,
                        ActiveConnections = a.ActiveConnections,
                        UniqueDevices = a.UniqueDevices,
                        BaselineConnections = a.BaselineConnections,
                        IsRural = a.IsRural,
                        IsNight = a.IsNight,
                        IsKnownEvent = a.IsKnownEvent,
                        IsSensitiveZone = a.IsSensitiveZone,
                        Latitude = a.Latitude,
                        Longitude = a.Longitude,
                        Title = a.Title,
                        Message = a.Message,
                        DetectedAtUtc = a.DetectedAtUtc,
                        ValidatedAtUtc = a.ValidatedAtUtc,
                        ValidatedByUserId = a.ValidatedByUserId,
                        Active = a.Active
                    })
                .ToList();

            var clusters = await _fusion.BuildClustersAsync(dtos, ct);

            foreach (var cluster in clusters)
            {
                cluster.RiskScore = _risk.ComputeZoneRisk(cluster);
            }

            return clusters;
        }

        public async Task<List<RiskZoneDTO>> GetRiskZonesAsync(CancellationToken ct = default)
        {
            var clusters = await GetActiveIncidentsAsync(ct);
            var zones = clusters
                    .Select(_risk.ToRiskZone)
                    .ToList();

            foreach (var zone in zones)
            {
                ct.ThrowIfCancellationRequested();

                var decisionContext =
                    new DecisionContext
                    {
                        RiskScore = zone.RiskScore,
                        Severity = (byte)Math.Clamp((int)zone.Severity, 0, byte.MaxValue),
                        ZoneName = zone.ZoneName,
                        Latitude = zone.Latitude,
                        Longitude = zone.Longitude,
                        ActiveConnections = zone.ActiveConnections,
                        HasCriticalWeather = zone.HasWeatherRisk,
                        HasTrafficIssue = zone.HasTrafficRisk
                    };

                await _officialEmergencyRiskContext.EnrichAsync(decisionContext, ct);

                var decision = await _decisionEngine.RecommendAsync(decisionContext, ct);

                zone.RiskScore = decision.EffectiveRiskScore;
                zone.Severity = decision.EffectiveSeverity;
                zone.HasOfficialEmergencyRisk = decisionContext.HasOfficialEmergencyRisk;
                zone.EmergencySourceCode = decisionContext.EmergencySourceCode;
                zone.EmergencyAlertIds = decisionContext.EmergencyAlertIds;
                zone.OfficialInstruction = decisionContext.OfficialInstruction;

                if (decisionContext.HasOfficialEmergencyRisk)
                {
                    zone.Recommendation = decisionContext.OfficialInstruction ?? decision.Message;
                }
            }

            return zones.OrderByDescending(z => z.RiskScore).ToList();
        }

        public async Task<DigitalTwinSnapshotDTO> GetDigitalTwinAsync(CancellationToken ct = default)
        {
            var clusters = await GetActiveIncidentsAsync(ct);

            return new DigitalTwinSnapshotDTO
            {
                GeneratedAtUtc = DateTime.UtcNow,

                Scope = "Wallonia",

                ActiveZones = clusters.Count,

                ActiveIncidents = clusters.Sum(c => c.AlertCount),

                Status = clusters.Any(c => c.Severity >= 4) ? "Critical" : clusters.Any() ? "Monitoring" : "Stable"
            };
        }
    }
}





































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.