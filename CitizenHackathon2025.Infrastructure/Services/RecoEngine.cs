using System.Globalization;
using System.Threading; 
using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Domain.Entities;

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

        public RecoEngine(
            IWeatherForecastService weatherService,
            ITrafficConditionService trafficService,
            ICrowdInfoService crowdService,
            IPlaceService placeService,
            ILogger<RecoEngine> logger)
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
        public async Task<List<Place>> RecommendAlternativesAsync(CancellationToken ct = default)
        {
            var recommendations = new List<Place>();

            var weather = await _weatherService.GetLatestWeatherForecastAsync(ct);
            var traffic = await _trafficService.GetLatestTrafficConditionAsync(ct); 
            var crowds = await _crowdService.GetAllCrowdInfoAsync(ct);               
            var places = await _placeService.GetLatestPlaceAsync(ct);                

            const int crowdedThreshold = 8;
            const decimal proximity = 0.0005m;

            foreach (var place in places)
            {
                var isIndoor = string.Equals(place.Indoor, "true", StringComparison.OrdinalIgnoreCase);

                var hasLat = decimal.TryParse(place.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var placeLat);
                var hasLon = decimal.TryParse(place.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var placeLon);

                var isCrowdedNearby = false;
                if (hasLat && hasLon)
                {
                    isCrowdedNearby = crowds.Any(c =>
                        Math.Abs(c.Latitude - placeLat) <= proximity &&
                        Math.Abs(c.Longitude - placeLon) <= proximity &&
                        c.CrowdLevel >= crowdedThreshold);
                }

                if (isIndoor && !isCrowdedNearby)
                    recommendations.Add(place);
            }

            _logger.LogInformation("✅ {Count} recommendations generated.", recommendations.Count);
            return recommendations;
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.