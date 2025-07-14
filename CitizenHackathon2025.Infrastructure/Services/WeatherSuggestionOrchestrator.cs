
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherSuggestionOrchestrator
    {
        private readonly IOpenWeatherService _weatherService;
        private readonly OpenAiSuggestionService _aiService;

        public WeatherSuggestionOrchestrator(IOpenWeatherService weatherService, OpenAiSuggestionService aiService)
        {
            _weatherService = weatherService;
            _aiService = aiService;
        }

        public async Task<string?> GetWeatherAndSuggestionsAsync(string city)
        {
            var coords = await _weatherService.GetCoordinatesAsync(city);
            if (coords == null) return "City not found.";

            var (lat, lon) = coords.Value;
            var weather = await _weatherService.GetWeatherAsync(lat, lon);
            if (weather == null) return "Weather data unavailable.";

            var weatherInfo = weather.MapToWeatherInfoDTO(city);
            var suggestion = await _aiService.GetSuggestionsAsync(weatherInfo);
            return suggestion ?? "No suggestions available.";
        }
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.