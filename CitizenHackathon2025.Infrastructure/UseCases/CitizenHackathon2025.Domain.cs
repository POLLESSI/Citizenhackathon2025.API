using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Extensions;

namespace CitizenHackathon2025.Infrastructure.UseCases
{
    public class CitizenSuggestionService
    {
        private readonly OpenAiSuggestionService _suggestionService;
        private readonly CrowdInfoService _crowdService;
        private readonly TrafficConditionService _trafficService;
        private readonly IOpenWeatherService _weatherService;
        private readonly IGeoService _geoService;
        private readonly ILogger<CitizenSuggestionService> _logger;

        public CitizenSuggestionService(OpenAiSuggestionService suggestionService, CrowdInfoService crowdService, TrafficConditionService trafficService, IOpenWeatherService weatherService, IGeoService geoService, ILogger<CitizenSuggestionService> logger)
        {
            _suggestionService = suggestionService;
            _crowdService = crowdService;
            _trafficService = trafficService;
            _weatherService = weatherService;
            _geoService = geoService;
            _logger = logger;
        }

        public async Task<SuggestionResponseDTO> GetPersonalizedSuggestionsAsync(string location, int id)
        {
            try
            {
                // Step 1: Geocode the city
                var coordinates = await _geoService.GetCoordinatesAsync(location);
                if (coordinates == null)
                {
                    _logger.LogWarning("Unable to geocode city : {Location}", location);
                    return new SuggestionResponseDTO
                    {
                        Location = location,
                        Error = "The city is not found or invalid."
                    };
                }
                

                (double lat, double lon) = coordinates.Value;

                // Step 2: Retrieve weather from coordinates
                var weather = await _weatherService.GetWeatherAsync(lat, lon);

                // Step 3: Retrieve crowd information by ID
                var crowd = await _crowdService.GetCrowdInfoByIdAsync(id);

                // Step 4: Call the AI ​​to generate a suggestion
                var weatherInfo = weather.MapToWeatherInfoDTO("Brussels");
                var aiSuggestion = await _suggestionService.GetSuggestionsAsync(weatherInfo);

                return new SuggestionResponseDTO
                {
                    Location = location,
                    //Weather = weather,
                    //CrowdInfo = crowd,
                    AiSuggestion = aiSuggestion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personalized suggestion.");
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