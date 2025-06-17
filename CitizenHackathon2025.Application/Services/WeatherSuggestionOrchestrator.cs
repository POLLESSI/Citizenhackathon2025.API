
namespace CitizenHackathon2025.Application.Services
{
    public class WeatherSuggestionOrchestrator
    {
        private readonly OpenWeatherService _weatherService;
        private readonly OpenAiSuggestionService _aiService;

        public WeatherSuggestionOrchestrator(OpenWeatherService weatherService, OpenAiSuggestionService aiService)
        {
            _weatherService = weatherService;
            _aiService = aiService;
        }

        public async Task<string?> GetWeatherAndSuggestionsAsync(string city)
        {
            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather == null) return "Unable to retrieve weather.";

            var suggestion = await _aiService.GetSuggestionsAsync(weather);
            return suggestion ?? "No suggestions available.";
        }
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.