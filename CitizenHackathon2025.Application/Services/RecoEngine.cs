using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.Services
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

            // 🧠 Example of simple logic: avoid outdoor places if it's raining
            foreach (var place in places)
            {
                //bool isGoodWeather = weather.TemperatureC >= 15 && weather.RainfallMm < 2;
                bool isCrowded = crowds.Any(c =>
                    c.Latitude == place.Latitude &&
                    c.Longitude == place.Longitude &&
                    c.CrowdLevel is "high" or "max");

                if (place.Indoor == "true" /*|| isGoodWeather*/)
                {
                    if (!isCrowded)
                        recommendations.Add(place);
                }
            }

            _logger.LogInformation("✅ {Count} recommandations générées.", recommendations.Count);
            return recommendations;
        }
    }
}
