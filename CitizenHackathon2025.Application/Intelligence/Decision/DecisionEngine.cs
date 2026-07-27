using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Application.Intelligence.Decision
{
    public sealed class DecisionEngine : IDecisionEngine
    {
        private readonly ILocalCrowdDecisionService _localCrowdDecision;
        private readonly ILogger<DecisionEngine> _logger;

        public DecisionEngine(ILocalCrowdDecisionService localCrowdDecision, ILogger<DecisionEngine> logger)
        {
            _localCrowdDecision = localCrowdDecision
                ?? throw new ArgumentNullException(nameof(localCrowdDecision));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DecisionRecommendation> RecommendAsync(DecisionContext context, CancellationToken ct = default)
        {
            var actions = new List<string>();

            if (context.RiskScore >= 85 || context.Severity >= 4)
            {
                actions.Add("Display a critical alert on the map.");
                actions.Add("Avoid recommending this area to users.");
                actions.Add("Suggest safer alternatives.");
                actions.Add("Request human validation.");
            }
            else if (context.RiskScore >= 65 || context.Severity == 3)
            {
                actions.Add("Monitor the area in real-time.");
                actions.Add("Display a warning to users.");
            }
            else
            {
                actions.Add("No immediate critical action required.");
            }

            if (context.HasCriticalWeather)
                actions.Add("Prioritize indoor alternatives.");

            if (context.HasTrafficIssue)
                actions.Add("Avoid routes through the affected area.");

            return new DecisionRecommendation
            {
                Priority = ResolvePriority(context.RiskScore, context.Severity),
                Actions = actions,
                Message = $"Decision recommendation generated for {context.ZoneName}."
            };
        }

        public async Task<List<DecisionActionDTO>> RecommendActionsAsync(IEnumerable<CrowdAlertCluster> clusters, CancellationToken ct = default)
        {
            var actions = new List<DecisionActionDTO>();

            foreach (var cluster in clusters)
            {
                ct.ThrowIfCancellationRequested();

                var localAiDecision = await TryAnalyzeWithLocalAiAsync(cluster, ct);

                var priority = ResolvePriority(cluster.RiskScore, cluster.Severity);

                var localMessage = string.IsNullOrWhiteSpace(localAiDecision?.UserMessage) ? null : localAiDecision.UserMessage.Trim();

                if (cluster.RiskScore >= 85 || cluster.Severity >= 4)
                {
                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "AvoidZone",
                        Message = localMessage
                            ?? "Avoid this area in user recommendations.",
                        RequiresHumanValidation = true
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "SuggestAlternatives",
                        Message = "Automatically suggest less crowded alternatives nearby.",
                        RequiresHumanValidation = false
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "NotifyModerator",
                        Message = "Request human validation by a moderator or administrator.",
                        RequiresHumanValidation = true
                    });
                }
                else if (cluster.RiskScore >= 65 || cluster.Severity == 3)
                {
                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "High",
                        ActionType = "DisplayWarning",
                        Message = localMessage
                            ?? "Display a discreet warning to users viewing this area.",
                        RequiresHumanValidation = false
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "High",
                        ActionType = "IncreaseMonitoring",
                        Message = "Increase monitoring of this area in the Command Center.",
                        RequiresHumanValidation = false
                    });
                }
                else if (cluster.RiskScore >= 40 || cluster.Severity == 2)
                {
                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Moderate",
                        ActionType = "Watch",
                        Message = localMessage
                            ?? "Keep this area under observation.",
                        RequiresHumanValidation = false
                    });
                }
            }

            return actions;
        }

        private async Task<LocalCrowdDecisionResult?> TryAnalyzeWithLocalAiAsync(CrowdAlertCluster cluster, CancellationToken ct)
        {
            try
            {
                var request = new LocalCrowdDecisionRequest
                {
                    ZoneName = cluster.ZoneName,
                    ActiveConnections = cluster.TotalActiveConnections,
                    UniqueDevices = cluster.TotalUniqueDevices,
                    BaselineConnections = null,
                    RiskScore = cluster.RiskScore,
                    Severity = cluster.Severity,
                    HasWeatherRisk = false,
                    HasTrafficRisk = false,
                    HasKnownEvent = false
                };

                var result = await _localCrowdDecision.AnalyzeAsync(request, ct);

                if (result is null)
                    return null;

                _logger.LogInformation("[LOCAL CROWD AI] Zone={ZoneName}, Priority={Priority}, Summary={Summary}", cluster.ZoneName, result.Priority, result.Summary);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LOCAL CROWD AI] Local analysis failed for zone {ZoneName}. Deterministic decision kept.", cluster.ZoneName);

                return null;
            }
        }

        private static string ResolvePriority(int riskScore, byte severity)
        {
            if (riskScore >= 85 || severity >= 4)
                return "Critical";

            if (riskScore >= 65 || severity == 3)
                return "High";

            if (riskScore >= 40 || severity == 2)
                return "Moderate";

            return "Normal";
        }
    }
}




























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.