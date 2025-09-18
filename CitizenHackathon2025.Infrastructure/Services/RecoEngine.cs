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
        public async Task<List<Place>> RecommendAlternativesAsync(int limit = 200, CancellationToken ct = default)
        {
            var recommendations = new List<Place>();

            var latestList = await _weatherService.GetLatestWeatherForecastAsync(ct);
            var latest = latestList?.FirstOrDefault();
            var traffic = await _trafficService.GetLatestTrafficConditionAsync(limit: 10, ct: ct);
            var crowds = await _crowdService.GetAllCrowdInfoAsync(limit: 200, ct: ct);
            var places = await _placeService.GetLatestPlaceAsync(limit: 200, ct: ct);

            const int crowdedThreshold = 8;
            const decimal proximity = 0.0005m;

            foreach (var place in places)
            {
                var isIndoor = place.Indoor; // ✅ bool direct

                var isCrowdedNearby = crowds.Any(c =>
                    Math.Abs(c.Latitude - place.Latitude) <= proximity &&
                    Math.Abs(c.Longitude - place.Longitude) <= proximity &&
                    c.CrowdLevel >= crowdedThreshold);

                if (isIndoor && !isCrowdedNearby)
                    recommendations.Add(place);
            }

            _logger.LogInformation("✅ {Count} recommendations generated.", recommendations.Count);
            return recommendations;
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.