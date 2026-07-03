namespace CitizenHackathon2025.Application.Intelligence.RiskAssessment
{
    public interface IRiskScoreCalculator
    {
        Task<RiskScoreResult> CalculateAsync(
            RiskScoreInput input,
            CancellationToken ct = default);
    }

    public sealed class RiskScoreInput
    {
        public int CrowdSeverity { get; set; }
        public int CrowdConnections { get; set; }
        public int WeatherSeverity { get; set; }
        public int TrafficSeverity { get; set; }
        public int EventCrowdLevel { get; set; }
        public int CitizenReports { get; set; }
        public int PredictionRisk { get; set; }
        public int DetectionConfidence { get; set; } = 70;
    }

    public sealed class RiskScoreResult
    {
        public int Score { get; set; }
        public string Level { get; set; } = "Normal";
        public List<string> Reasons { get; set; } = new();
    }
}




























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.