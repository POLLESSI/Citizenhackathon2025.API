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
    }
}




























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.