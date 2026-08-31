using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class OpenAiSuggestionService
    {
        //private readonly OpenAIAPI _openAi;
        private readonly MemoryCacheService _cache;
        private readonly ILogger<OpenAiSuggestionService> _logger;

        public OpenAiSuggestionService(IConfiguration config, MemoryCacheService cache, ILogger<OpenAiSuggestionService> logger)
        {
            //_openAi = new OpenAIAPI(config["OpenAI:ApiKey"]);
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetSuggestionsAsync(WeatherInfoDTO weather)
        {
            string cacheKey = $"suggestions-{weather.Location}-{weather.WeatherDescription}-{weather.TemperatureCelsius}";

            return await _cache.GetOrAddAsync(cacheKey, async () => { 
                try
                {
                    // Simulate a call to OpenAI API to get suggestions based on weather
                    // This is where you would implement the actual OpenAI API call logic
                    // For now, we return a placeholder string
                    // Example: return "Visit the local museum and enjoy the exhibits!";
                    return $"Based on the weather in {weather.Location}, I suggest you visit the local park and enjoy a picnic!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error with OpenAI.");
                    return null;
                }
            }, TimeSpan.FromMinutes(15));
            
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.