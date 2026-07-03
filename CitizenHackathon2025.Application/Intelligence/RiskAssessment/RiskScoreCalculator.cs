namespace CitizenHackathon2025.Application.Intelligence.RiskAssessment
{
    public sealed class RiskScoreCalculator : IRiskScoreCalculator
    {
        public Task<RiskScoreResult> CalculateAsync(
            RiskScoreInput input,
            CancellationToken ct = default)
        {
            var score = 0;
            var reasons = new List<string>();

            if (input.CrowdSeverity > 0)
            {
                var points = input.CrowdSeverity * 15;
                score += points;
                reasons.Add($"Crowd severity: +{points}");
            }

            if (input.CrowdConnections >= 1000)
            {
                score += 15;
                reasons.Add("Very high crowd connection count: +15");
            }
            else if (input.CrowdConnections >= 500)
            {
                score += 10;
                reasons.Add("High crowd connection count: +10");
            }
            else if (input.CrowdConnections >= 250)
            {
                score += 5;
                reasons.Add("Moderate crowd connection count: +5");
            }

            if (input.WeatherSeverity > 0)
            {
                var points = input.WeatherSeverity * 5;
                score += points;
                reasons.Add($"Weather severity: +{points}");
            }

            if (input.TrafficSeverity > 0)
            {
                var points = input.TrafficSeverity * 5;
                score += points;
                reasons.Add($"Traffic severity: +{points}");
            }

            if (input.EventCrowdLevel > 0)
            {
                var points = input.EventCrowdLevel * 4;
                score += points;
                reasons.Add($"Event crowd level: +{points}");
            }

            if (input.CitizenReports >= 3)
            {
                score += 10;
                reasons.Add("Multiple citizen reports: +10");
            }

            if (input.PredictionRisk > 0)
            {
                var points = Math.Clamp(input.PredictionRisk, 0, 15);
                score += points;
                reasons.Add($"Prediction risk: +{points}");
            }

            if (input.DetectionConfidence < 50)
            {
                score -= 10;
                reasons.Add("Low detection confidence: -10");
            }
            else if (input.DetectionConfidence >= 85)
            {
                score += 5;
                reasons.Add("High detection confidence: +5");
            }

            score = Math.Clamp(score, 0, 100);

            return Task.FromResult(new RiskScoreResult
            {
                Score = score,
                Level = score switch
                {
                    >= 85 => "Critical",
                    >= 65 => "High",
                    >= 40 => "Moderate",
                    >= 20 => "Low",
                    _ => "Normal"
                },
                Reasons = reasons
            });
        }
    }
}






































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.