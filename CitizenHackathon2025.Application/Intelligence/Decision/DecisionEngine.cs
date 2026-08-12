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

        public Task<DecisionRecommendation> RecommendAsync(DecisionContext context, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            ct.ThrowIfCancellationRequested();

            var actions = new List<string>();

            var effectiveRiskScore = context.RiskScore;

            var effectiveSeverity = context.Severity;


            // =====================================================
            // OFFICIAL EMERGENCY OVERRIDE
            // =====================================================

            if (context.HasOfficialEmergencyRisk)
            {
                /*
                 * An official alert does not merely add a few
                 * points to the heuristic score.
                 *
                 * It imposes a minimum risk level.
                 */
                effectiveRiskScore = ApplyOfficialAlertFloor(
                        currentScore: context.RiskScore,
                        officialSeverity: context.OfficialEmergencySeverity,
                        immediate: context.IsOfficialEmergencyImmediate);

                effectiveSeverity = Math.Max(context.Severity, context.OfficialEmergencySeverity);


                actions.Add(!string.IsNullOrWhiteSpace(context.EmergencySourceCode)
                    ? $"Official emergency alert active " + $"({context.EmergencySourceCode})." : "Official emergency alert active.");

                /*
                 * Preserve the official wording.
                 *
                 * OutZen may present it but must not reinterpret
                 * it as its own instruction.
                 */
                if (!string.IsNullOrWhiteSpace(context.OfficialInstruction))
                {
                    actions.Add($"Official instruction: " + $"{context.OfficialInstruction}");
                }
                actions.Add("Do not recommend destinations inside " + "the affected official alert zone.");
                actions.Add("Do not route users through the " + "affected official alert zone.");
            }


            // =====================================================
            // GLOBAL DETERMINISTIC DECISION
            // =====================================================

            if (effectiveRiskScore >= 85 || effectiveSeverity >= 4)
            {
                actions.Add("Display a critical alert on the map.");
                actions.Add("Avoid recommending this area to users.");
                actions.Add("Suggest safer alternatives.");
                actions.Add("Request human validation.");
            }
            else if (effectiveRiskScore >= 65 || effectiveSeverity == 3)
            {
                actions.Add("Monitor the area in real-time.");
                actions.Add("Display a warning to users.");
            }
            else
            {
                actions.Add("No immediate critical action required.");
            }


            // =====================================================
            // CONTEXTUAL COMPLEMENTS
            // =====================================================

            if (context.HasCriticalWeather)
            {
                actions.Add("Prioritize indoor alternatives.");
            }


            if (context.HasTrafficIssue)
            {
                actions.Add("Avoid routes through the affected area.");
            }


            return Task.FromResult(
                new DecisionRecommendation
                {
                    Priority = ResolvePriority(effectiveRiskScore, effectiveSeverity),
                    EffectiveRiskScore = effectiveRiskScore,
                    EffectiveSeverity = effectiveSeverity,
                    Actions = actions,
                    Message =
                        context.HasOfficialEmergencyRisk
                            ? $"Official emergency-aware decision " +
                              $"generated for {context.ZoneName}."
                            : $"Decision recommendation generated " +
                              $"for {context.ZoneName}."
                });
        }

        public async Task<List<DecisionActionDTO>> RecommendActionsAsync(IEnumerable<CrowdAlertCluster> clusters, CancellationToken ct = default)
        {
            var actions = new List<DecisionActionDTO>();

            foreach (var cluster in clusters)
            {
                ct.ThrowIfCancellationRequested();

                var localAiDecision = await TryAnalyzeWithLocalAiAsync(cluster, ct);

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

        private static int ApplyOfficialAlertFloor(int currentScore, byte officialSeverity, bool immediate)
        {
            var minimumScore = officialSeverity switch
                {
                    >= 4 => 95,
                    3 => immediate ? 90 : 85,
                    2 => 65,
                    1 => 40,
                    _ => currentScore
                };

            return Math.Max(currentScore, minimumScore);
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