using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.RiskAssessment
{
    public sealed class RiskScoreCalculator : IRiskScoreCalculator
    {
        public int ComputeZoneRisk(CrowdAlertCluster cluster)
        {
            var score = 0;

            score += cluster.Severity switch
            {
                >= 4 => 45,
                3 => 30,
                2 => 18,
                1 => 8,
                _ => 0
            };

            score += cluster.TotalActiveConnections switch
            {
                >= 3000 => 25,
                >= 1500 => 18,
                >= 750 => 12,
                >= 300 => 8,
                >= 100 => 4,
                _ => 0
            };

            score += Math.Min(15, cluster.AlertCount * 4);

            return Math.Clamp(score, 0, 100);
        }

        public RiskZoneDTO ToRiskZone(CrowdAlertCluster cluster)
        {
            var score = ComputeZoneRisk(cluster);

            return new RiskZoneDTO
            {
                ZoneName = cluster.ZoneName,
                Latitude = cluster.Latitude,
                Longitude = cluster.Longitude,
                RiskScore = score,
                Severity = cluster.Severity,
                ActiveConnections = cluster.TotalActiveConnections,
                HasCrowdRisk = cluster.TotalActiveConnections > 0,
                Recommendation = score switch
                {
                    >= 85 => "Avoid the area and suggest alternatives.",
                    >= 65 => "Enhanced monitoring recommended.",
                    >= 40 => "User warning recommended.",
                    _ => "Normal situation."
                }
            };
        }
    }
}






































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.