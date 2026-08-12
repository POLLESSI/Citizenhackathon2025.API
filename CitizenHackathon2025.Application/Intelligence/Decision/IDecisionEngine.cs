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
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int ActiveConnections { get; set; }

        public bool HasCriticalWeather { get; set; }

        public bool HasTrafficIssue { get; set; }


        // =====================================================
        // OFFICIAL EMERGENCY INTELLIGENCE
        // =====================================================

        /// <summary>
        /// True when an active official emergency alert
        /// affects the current decision zone.
        /// </summary>
        public bool HasOfficialEmergencyRisk { get; set; }


        /// <summary>
        /// Example: BE-ALERT, BE-NCCN.
        /// </summary>
        public string? EmergencySourceCode { get; set; }


        /// <summary>
        /// Normalized OutZen severity:
        /// 0 = unknown
        /// 1 = minor
        /// 2 = moderate
        /// 3 = severe
        /// 4 = critical/extreme
        /// </summary>
        public byte OfficialEmergencySeverity { get; set; }


        /// <summary>
        /// True when the normalized official alert
        /// has immediate urgency.
        /// </summary>
        public bool IsOfficialEmergencyImmediate { get; set; }


        public IReadOnlyList<Guid> EmergencyAlertIds { get; set; }
            = Array.Empty<Guid>();


        /// <summary>
        /// Official safety instruction.
        /// Must not be rewritten as an OutZen instruction.
        /// </summary>
        public string? OfficialInstruction { get; set; }
    }

    public sealed class DecisionRecommendation
    {
        public string Priority { get; set; } = "Normal";
        public List<string> Actions { get; set; } = new();
        public string Message { get; set; } = "";
        public int EffectiveRiskScore { get; set; }

        public byte EffectiveSeverity { get; set; }
    }
}










































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.