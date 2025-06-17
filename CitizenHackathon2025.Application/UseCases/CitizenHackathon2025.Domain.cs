using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Services;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.DTOs;
using CitizenHackathon2025.Application.Services;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.UseCases
{
    public class CitizenHackathon2025
    {
        private readonly OpenAiSuggestionService _suggestionService;
        private readonly CrowdInfoService _crowdService;
        private readonly TrafficConditionService _trafficService;
        private readonly OpenWeatherService _weatherService;
        private readonly ILogger<CitizenHackathon2025> _logger;

        public CitizenHackathon2025(
            OpenAiSuggestionService suggestionService,
            CrowdInfoService crowdService,
            TrafficConditionService trafficService,
            OpenWeatherService weatherService,
            ILogger<CitizenHackathon2025> logger)
        {
            _suggestionService = suggestionService;
            _crowdService = crowdService;
            _trafficService = trafficService;
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<SuggestionResponseDTO> GetPersonalizedSuggestionsAsync(string location, int id)
        {
        #nullable disable
            try
            {
                var weather = await _weatherService.GetWeatherAsync(location);
                var crowd = await _crowdService.GetCrowdInfoByIdAsync(id);
                //var traffic = await _trafficService.UpdateTrafficCondition(
                //        new TrafficConditionDTO
                //        {
                //            LocationName = location,
                //            Latitude = crowd.Latitude,
                //            Longitude = crowd.Longitude
                //        }
                //    );

                // AI suggestion based on all factors
                var aiSuggestion = await _suggestionService.GetSuggestionsAsync(weather);

                return new SuggestionResponseDTO
                {
                    Location = location,
                    Weather = weather,
                    //CrowdInfo = crowd,
                    //TrafficInfo = traffic,
                    AiSuggestion = aiSuggestion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the main scenario.");
                return new SuggestionResponseDTO
                {
                    Location = location,
                    Error = "An error occurred while generating the suggestion."
                };
            }
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.