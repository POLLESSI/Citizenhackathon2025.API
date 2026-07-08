using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.Decision
{
    public sealed class DecisionEngine : IDecisionEngine
    {
        public Task<DecisionRecommendation> RecommendAsync(DecisionContext context, CancellationToken ct = default)
        {
            var actions = new List<string>();

            if (context.RiskScore >= 85 || context.Severity >= 4)
            {
                actions.Add("Afficher une alerte critique sur la carte.");
                actions.Add("Éviter de recommander cette zone aux utilisateurs.");
                actions.Add("Proposer des alternatives plus sûres.");
                actions.Add("Demander une validation humaine.");
            }
            else if (context.RiskScore >= 65 || context.Severity == 3)
            {
                actions.Add("Surveiller la zone en temps réel.");
                actions.Add("Afficher un avertissement aux utilisateurs.");
            }
            else
            {
                actions.Add("Aucune action critique immédiate.");
            }

            if (context.HasCriticalWeather)
                actions.Add("Prioriser les alternatives indoor.");

            if (context.HasTrafficIssue)
                actions.Add("Éviter les itinéraires traversant la zone affectée.");

            return Task.FromResult(new DecisionRecommendation
            {
                Priority = ResolvePriority(context.RiskScore, context.Severity),
                Actions = actions,
                Message = $"Decision recommendation generated for {context.ZoneName}."
            });
        }

        private static string ResolvePriority(int riskScore, byte severity)
        {
            if (riskScore >= 85 || severity >= 4) return "Critical";
            if (riskScore >= 65 || severity == 3) return "High";
            if (riskScore >= 40 || severity == 2) return "Moderate";
            return "Normal";
        }

        public Task<List<DecisionActionDTO>> RecommendActionsAsync(IEnumerable<CrowdAlertCluster> clusters, CancellationToken ct = default)
        {
            var actions = new List<DecisionActionDTO>();

            foreach (var cluster in clusters)
            {
                ct.ThrowIfCancellationRequested();

                if (cluster.RiskScore >= 85 || cluster.Severity >= 4)
                {
                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "AvoidZone",
                        Message = "Éviter cette zone dans les recommandations utilisateur.",
                        RequiresHumanValidation = true
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "SuggestAlternatives",
                        Message = "Proposer automatiquement des alternatives moins fréquentées à proximité.",
                        RequiresHumanValidation = false
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "Critical",
                        ActionType = "NotifyModerator",
                        Message = "Demander une validation humaine par un modérateur ou administrateur.",
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
                        Message = "Afficher un avertissement discret aux utilisateurs consultant cette zone.",
                        RequiresHumanValidation = false
                    });

                    actions.Add(new DecisionActionDTO
                    {
                        ZoneName = cluster.ZoneName,
                        RiskScore = cluster.RiskScore,
                        Severity = cluster.Severity,
                        Priority = "High",
                        ActionType = "IncreaseMonitoring",
                        Message = "Renforcer la surveillance de cette zone dans le Command Center.",
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
                        Message = "Maintenir cette zone sous observation.",
                        RequiresHumanValidation = false
                    });
                }
            }

            return Task.FromResult(actions);
        }
    }
}




























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.