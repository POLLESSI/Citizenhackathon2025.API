using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.Prediction
{
    public sealed class PredictionEngine : IPredictionEngine
    {
        public Task<List<PredictionDTO>> PredictAsync(
            IEnumerable<RiskZoneDTO> zones,
            CancellationToken ct = default)
        {
            var result = zones.Select(z =>
            {
                var growth15 = z.HasEventRisk ? 10 : 5;
                var growth30 = z.HasTrafficRisk || z.HasWeatherRisk ? 20 : 12;

                var risk15 = Math.Clamp(z.RiskScore + growth15, 0, 100);
                var risk30 = Math.Clamp(z.RiskScore + growth30, 0, 100);

                return new PredictionDTO
                {
                    ZoneName = z.ZoneName,
                    Latitude = z.Latitude,
                    Longitude = z.Longitude,
                    CurrentRiskScore = z.RiskScore,
                    PredictedRiskScore15Min = risk15,
                    PredictedRiskScore30Min = risk30,
                    SaturationLikely = risk15 >= 85 || risk30 >= 85,
                    Explanation = risk30 >= 85
                        ? "Likely saturation within the next 30 minutes."
                        : "No critical saturation predicted in the short term."
                };
            }).ToList();

            return Task.FromResult(result);
        }
    }
}




































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.