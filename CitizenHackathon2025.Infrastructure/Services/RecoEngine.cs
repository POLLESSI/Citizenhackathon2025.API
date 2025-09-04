using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Domain.Entities;
using System.Globalization;

namespace CitizenHackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Recommendation engine to offer smart alternatives.
    /// </summary>
    public class RecoEngine
    {
        private readonly IWeatherForecastService _weatherService;
        private readonly ITrafficConditionService _trafficService;
        private readonly ICrowdInfoService _crowdService;
        private readonly IPlaceService _placeService;
        private readonly ILogger<RecoEngine> _logger;

        public RecoEngine(IWeatherForecastService weatherService, ITrafficConditionService trafficService, ICrowdInfoService crowdService, IPlaceService placeService, ILogger<RecoEngine> logger)
        {
            _weatherService = weatherService;
            _trafficService = trafficService;
            _crowdService = crowdService;
            _placeService = placeService;
            _logger = logger;
        }
        /// <summary>
        /// Offers recommended alternatives based on weather, traffic and crowds.
        /// </summary>
        public async Task<List<Place>> RecommendAlternativesAsync()
        {
            var recommendations = new List<Place>();

            var weather = await _weatherService.GetLatestWeatherForecastAsync();
            var traffic = await _trafficService.GetLatestTrafficConditionAsync();
            var crowds = await _crowdService.GetAllCrowdInfoAsync();
            var places = await _placeService.GetLatestPlaceAsync();

            // Crowded threshold (e.g. 8/10)
            const int crowdedThreshold = 8;

            // To compare positions, we avoid strict equality: we use a proximity threshold
            // ~ 100 m → ~ 0.001 deg ; we take 0.0005 for ~50 m
            const decimal proximity = 0.0005m;

            // 🧠 Example of simple logic: avoid outdoor places if it's raining
            foreach (var place in places)
            {
                // Normalize Indoor (if string)
                var isIndoor = string.Equals(place.Indoor, "true", StringComparison.OrdinalIgnoreCase);

                // Parse lat/lon of the location if they are string
                var hasLat = decimal.TryParse(place.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var placeLat);
                var hasLon = decimal.TryParse(place.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var placeLon);

                // Find out if there is a crowd nearby above the threshold
                var isCrowdedNearby = false;
                if (hasLat && hasLon)
                {
                    isCrowdedNearby = crowds.Any(c =>
                        Math.Abs(c.Latitude - placeLat) <= proximity &&
                        Math.Abs(c.Longitude - placeLon) <= proximity &&
                        c.CrowdLevel >= crowdedThreshold);
                }

                // Simple example: prefer indoors if it rains, avoid crowded places
                // var isGoodWeather = weather.TemperatureC >= 15 && weather.RainfallMm < 2;

                if (isIndoor /*|| isGoodWeather*/)
                {
                    if (!isCrowdedNearby)
                        recommendations.Add(place);
                }
            }

            _logger.LogInformation("✅ {Count} recommendations generated.", recommendations.Count);
            return recommendations;
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.