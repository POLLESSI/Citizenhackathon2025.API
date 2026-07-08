using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.Decision
{
    public interface IDecisionEngine
    {
        Task<DecisionRecommendation> RecommendAsync(DecisionContext context, CancellationToken ct = default);
        Task<List<DecisionActionDTO>> RecommendActionsAsync(IEnumerable<CrowdAlertCluster> clusters, CancellationToken ct = default);
    }

    public sealed class DecisionContext
    {
        public int RiskScore { get; set; }
        public byte Severity { get; set; }
        public string ZoneName { get; set; } = "";
        public int ActiveConnections { get; set; }
        public bool HasCriticalWeather { get; set; }
        public bool HasTrafficIssue { get; set; }
    }

    public sealed class DecisionRecommendation
    {
        public string Priority { get; set; } = "Normal";
        public List<string> Actions { get; set; } = new();
        public string Message { get; set; } = "";
    }
}










































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.