using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherSuggestionOrchestrator
    {
        private readonly IOpenWeatherService _weatherService;
        private readonly OpenAiSuggestionService _aiService;
        private readonly IMongoSnapshotWriter _mongoSnapshotWriter;
        private readonly ILogger<WeatherSuggestionOrchestrator> _logger;

        public WeatherSuggestionOrchestrator(IOpenWeatherService weatherService, OpenAiSuggestionService aiService, IMongoSnapshotWriter mongoSnapshotWriter, ILogger<WeatherSuggestionOrchestrator> logger)
        {
            _weatherService = weatherService;
            _aiService = aiService;
            _mongoSnapshotWriter = mongoSnapshotWriter;
            _logger = logger;
        }

        public async Task<string?> GetWeatherAndSuggestionsAsync(string city)
        {
            try
            {
                var coords = await _weatherService.GetCoordinatesAsync(city);
                if (coords == null)
                    return "City not found.";

                var (lat, lon) = coords.Value;

                var weather = await _weatherService.GetWeatherAsync(lat, lon);
                if (weather == null)
                    return "Weather data unavailable.";

                var weatherInfo = weather.MapToWeatherInfoDTO(city);

                var suggestion = await _aiService.GetSuggestionsAsync(weatherInfo);

                return suggestion ?? "No suggestions available.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate weather suggestions for city {City}.", city);

                return "Unable to generate weather suggestions at the moment.";
            }
        }
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.