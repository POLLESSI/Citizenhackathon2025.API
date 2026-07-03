using CitizenHackathon2025.Application.Intelligence.Prediction;

namespace CitizenHackathon2025.Application.Intelligence.Prediction
{
    public sealed class PredictionEngine : IPredictionEngine
    {
        public Task<CrowdPredictionResult> PredictCrowdAsync(
            IReadOnlyList<CrowdPredictionPoint> history,
            CancellationToken ct = default)
        {
            if (history is null || history.Count == 0)
                return Task.FromResult(new CrowdPredictionResult());

            var ordered = history.OrderBy(x => x.TimestampUtc).ToList();
            var latest = ordered[^1];

            if (ordered.Count < 2)
            {
                return Task.FromResult(new CrowdPredictionResult
                {
                    CurrentConnections = latest.ActiveConnections,
                    PredictedConnections15Minutes = latest.ActiveConnections,
                    PredictedConnections30Minutes = latest.ActiveConnections,
                    Confidence = 30
                });
            }

            var first = ordered[0];
            var elapsedMinutes = Math.Max(1d, (latest.TimestampUtc - first.TimestampUtc).TotalMinutes);
            var growthPerMinute = (latest.ActiveConnections - first.ActiveConnections) / elapsedMinutes;

            return Task.FromResult(new CrowdPredictionResult
            {
                CurrentConnections = latest.ActiveConnections,
                PredictedConnections15Minutes = Math.Max(0, (int)Math.Round(latest.ActiveConnections + growthPerMinute * 15)),
                PredictedConnections30Minutes = Math.Max(0, (int)Math.Round(latest.ActiveConnections + growthPerMinute * 30)),
                GrowthPerMinute = growthPerMinute,
                Confidence = ordered.Count >= 6 ? 70 : 50
            });
        }
    }
}




































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.