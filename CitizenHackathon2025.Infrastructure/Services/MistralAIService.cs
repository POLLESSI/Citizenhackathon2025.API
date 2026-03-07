using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.Extensions;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class MistralAIService : IMistralAIService
    {
    #nullable disable
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<MistralAIService> _logger;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMemoryCache _cache;

        public MistralAIService(HttpClient httpClient, IConfiguration config, ILogger<MistralAIService> logger, ISuggestionRepository suggestionRepository, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _suggestionRepository = suggestionRepository;
            _cache = cache;
        }

        public async Task<string> GenerateSuggestionAsync(string prompt, CancellationToken ct = default)
        {
            var apiUrl = _config["MistralAI:ApiUrl"]; // "http://localhost:11434/api/chat" (Ollama)
            var model = _config["MistralAI:Model"];   // "Mistral" (model name in Ollama)
            var cacheKey = $"Mistral_{prompt.Hash()}";

            if (_cache.TryGetValue(cacheKey, out string cachedResponse))
                return cachedResponse;

            var responseCache = await CallOllamaApi(prompt, ct);
            _cache.Set(cacheKey, responseCache, TimeSpan.FromHours(1));
            return responseCache;

            // Request format for Ollama (different from Mistral Cloud)
            var request = new
            {
                model,
                messages = new[]
                {
            new { role = "user", content = prompt }
        },
                stream = false, // Disable streaming for a complete response
                options = new { temperature = 0.7f, num_predict = 500 }
            };

            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization"); // Ollama local does not need an API key
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CitizenHackathon2025/1.0");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(240)); // ✅ Local timeout of 240s

                var response = await _httpClient.PostAsJsonAsync(apiUrl, request, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadFromJsonAsync<JsonDocument>(cts.Token);
                return responseContent?.RootElement.GetProperty("message").GetProperty("content").GetString()
                       ?? "No response from Ollama (timeout or error).";
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Ollama request timed out after 240s.");
                return "Request to Ollama timed out. Please try again with a shorter prompt.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama (Mistral local)");
                throw;
            }
        }
        public async Task<string> CallOllamaApi(string prompt, CancellationToken ct)
        {
            var apiUrl = _config["MistralAI:ApiUrl"]; // "http://localhost:11434/api/chat"
            var model = _config["MistralAI:Model"];   // "mistral"

            var request = new
            {
                model,
                messages = new[]
                {
            new { role = "user", content = prompt }
        },
                stream = false,
                options = new
                {
                    temperature = _config.GetValue<float>("MistralAI:Temperature", 0.7f)
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(apiUrl, request, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
                return responseContent?.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No response generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama error");
                throw;
            }
        }

        // Implementation of other interface methods (basic examples)
        public Task<int> ArchivePastGptInteractionsAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteSuggestionAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync(string prompt, CancellationToken ct = default)
        {
            var suggestionText = await GenerateSuggestionAsync(prompt, ct);
            // Example of a basic mapping (to be adapted according to your business logic)
            return new List<Suggestion>
            {
                new Suggestion
                {
                    Message = suggestionText,
                    Context = prompt,
                    DateSuggestion = DateTime.UtcNow
                }
            };
        }

        public Task<Suggestion?> GetSuggestionByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default)
        {
            var prompt = $"Generates a weather report for {location} in compliance with the GDPR.";
            var suggestionText = await GenerateSuggestionAsync(prompt, ct);
            return new List<Suggestion>
            {
                new Suggestion
                {
                    Message = suggestionText,
                    LocationName = location,
                    DateSuggestion = DateTime.UtcNow
                }
            };
        }

        public Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetRecommendationsForSwimmingAreasAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves a suggestion to the database.
        /// </summary>
        /// <param name="suggestion">The suggestion to save.</param>
        /// <returns>The suggestion is saved, or null in case of error.</returns>
        public async Task<Suggestion?> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default)
        {
            if (suggestion == null)
            {
                _logger.LogWarning("Attempting to save a null suggestion.");
                return null;
            }

            try
            {
                // Basic validation
                if (suggestion.User_Id <= 0)
                {
                    _logger.LogWarning("Invalid User_Id for the suggestion.");
                    return null;
                }

                // Data normalization
                if (suggestion.Latitude.HasValue)
                    suggestion.Latitude = (double)MapperExtensions.RoundLat(suggestion.Latitude.Value);
                if (suggestion.Longitude.HasValue)
                    suggestion.Longitude = (double)MapperExtensions.RoundLon(suggestion.Longitude.Value);
                suggestion.DateSuggestion = MapperExtensions.TruncateToSecond(suggestion.DateSuggestion);

                // Backup via repository
                return await _suggestionRepository.SaveSuggestionAsync(suggestion, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving suggestion.");
                return null;
            }
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.