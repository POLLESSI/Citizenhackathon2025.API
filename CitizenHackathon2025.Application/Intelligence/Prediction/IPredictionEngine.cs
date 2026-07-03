namespace CitizenHackathon2025.Application.Intelligence.Prediction
{
    public interface IPredictionEngine
    {
        Task<CrowdPredictionResult> PredictCrowdAsync(IReadOnlyList<CrowdPredictionPoint> history, CancellationToken ct = default);
    }

    public sealed class CrowdPredictionPoint
    {
        public DateTime TimestampUtc { get; set; }
        public int ActiveConnections { get; set; }
        public int UniqueDevices { get; set; }
    }

    public sealed class CrowdPredictionResult
    {
        public int CurrentConnections { get; set; }
        public int PredictedConnections15Minutes { get; set; }
        public int PredictedConnections30Minutes { get; set; }
        public double GrowthPerMinute { get; set; }
        public int Confidence { get; set; }
    }
}




























































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.