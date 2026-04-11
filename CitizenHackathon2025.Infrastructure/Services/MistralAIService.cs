using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class MistralAIService : IMistralAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<MistralAIService> _logger;
        private readonly MistralContextBuilder _contextBuilder;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMemoryCache _cache;
        private readonly ILocalAiContextService _localAiContextService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MistralAIService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<MistralAIService> logger,
            MistralContextBuilder contextBuilder,
            ISuggestionRepository suggestionRepository,
            IMemoryCache cache,
            ILocalAiContextService localAiContextService)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _contextBuilder = contextBuilder;
            _suggestionRepository = suggestionRepository;
            _cache = cache;
            _localAiContextService = localAiContextService;
        }

        public async Task<string> GenerateSuggestionAsync(string prompt, double? latitude, double? longitude, CancellationToken ct = default)
        {
            try
            {
                var model = _config["MistralAI:Model"] ?? "mistral";

                var localContext = await _localAiContextService.BuildContextAsync(prompt, latitude, longitude, ct);
                var groundedPrompt = _localAiContextService.BuildPrompt(localContext);

                var request = new
                {
                    model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a reliable local OutZen assistant. You never invent information that is not in context."
                        },
                        new
                        {
                            role = "user",
                            content = groundedPrompt
                        }
                    },
                    stream = false,
                    options = new
                    {
                        temperature = _config.GetValue<float?>("MistralAI:Temperature") ?? 0.3f
                    }
                };

                using var response = await _httpClient.PostAsJsonAsync("api/chat", request, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Ollama status code: {StatusCode}", (int)response.StatusCode);
                _logger.LogDebug("Ollama raw response: {Response}", responseContent);

                response.EnsureSuccessStatusCode();

                var jsonResponse = JsonSerializer.Deserialize<MistralResponse>(responseContent, JsonOptions);
                return jsonResponse?.Message?.Content?.Trim() ?? "No response from Mistral.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Mistral API");
                throw;
            }
        }

        public async Task<string> CallOllamaApi(string prompt, CancellationToken ct)
        {
            var endpoint = _config["MistralAI:ApiUrl"] ?? "http://localhost:11434/api/chat";
            var model = _config["MistralAI:Model"] ?? "mistral";

            var request = new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                stream = false,
                options = new { temperature = _config.GetValue<float>("MistralAI:Temperature", 0.7f) }
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(endpoint, request, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var jsonResponse = JsonSerializer.Deserialize<MistralResponse>(responseContent, JsonOptions);

                return jsonResponse?.Message?.Content ?? "No response from Mistral.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama error");
                throw;
            }
        }

        public async Task<string> StreamSuggestionAsync(
            string prompt,
            double? latitude,
            double? longitude,
            Func<GptResponseChunkDto, Task> onChunk,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(onChunk);

            var model = _config["MistralAI:Model"] ?? "mistral";
            var temperature = _config.GetValue<float?>("MistralAI:Temperature") ?? 0.3f;

            var localContext = await _localAiContextService.BuildContextAsync(prompt, latitude, longitude, ct);
            var groundedPrompt = _localAiContextService.BuildPrompt(localContext);

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a reliable local OutZen assistant. You never invent information that is not in context."
                    },
                    new
                    {
                        role = "user",
                        content = groundedPrompt
                    }
                },
                stream = true,
                options = new
                {
                    temperature
                }
            };

            var accumulated = new StringBuilder();

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            _logger.LogInformation("Ollama streaming status code: {StatusCode}", (int)response.StatusCode);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(ct);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                OllamaChatStreamResponse? chunkEnvelope;
                try
                {
                    chunkEnvelope = JsonSerializer.Deserialize<OllamaChatStreamResponse>(line, JsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize Ollama stream line: {Line}", line);
                    continue;
                }

                if (chunkEnvelope is null)
                    continue;

                var chunkText = chunkEnvelope.Message?.Content ?? string.Empty;
                if (!string.IsNullOrEmpty(chunkText))
                {
                    accumulated.Append(chunkText);

                    await onChunk(new GptResponseChunkDto
                    {
                        Chunk = chunkText,
                        IsFinal = false
                    });
                }

                if (chunkEnvelope.Done)
                    break;
            }

            var finalText = accumulated.ToString().Trim();
            return string.IsNullOrWhiteSpace(finalText)
                ? "No response from Mistral."
                : finalText;
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync(string prompt, CancellationToken ct = default)
        {
            var suggestionText = await GenerateSuggestionAsync(prompt, null, null, ct);

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

        public async Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default)
        {
            double? lat = 50.8503;
            double? lon = 4.3517;

            var prompt = $"Generates a weather report for {location} in compliance with the GDPR.";
            var suggestionText = await GenerateSuggestionAsync(prompt, lat, lon, ct);

            return new List<Suggestion>
            {
                new Suggestion
                {
                    Message = suggestionText,
                    LocationName = location,
                    DateSuggestion = DateTime.UtcNow,
                    Latitude = lat,
                    Longitude = lon
                }
            };
        }

        public Task<int> ArchivePastGptInteractionsAsync() => Task.FromResult(0);
        public Task DeleteSuggestionAsync(int id) => Task.CompletedTask;
        public Task<Suggestion?> GetSuggestionByIdAsync(int id) => Task.FromResult<Suggestion?>(null);
        public Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id) => Task.FromResult(Enumerable.Empty<Suggestion>());
        public Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id) => Task.FromResult(Enumerable.Empty<Suggestion>());
        public Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id) => Task.FromResult(Enumerable.Empty<Suggestion>());
        public Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetRecommendationsForSwimmingAreasAsync() => Task.FromResult(Enumerable.Empty<SuggestionGroupedByPlaceDTO>());

        public async Task<Suggestion?> SaveSuggestionAsync(Suggestion suggestion, CancellationToken ct = default)
        {
            if (suggestion == null)
            {
                _logger.LogWarning("Attempting to save a null suggestion.");
                return null;
            }

            try
            {
                if (suggestion.User_Id <= 0)
                {
                    _logger.LogWarning("Invalid User_Id for the suggestion.");
                    return null;
                }

                if (suggestion.Latitude.HasValue)
                    suggestion.Latitude = (double)MapperExtensions.RoundLat(suggestion.Latitude.Value);

                if (suggestion.Longitude.HasValue)
                    suggestion.Longitude = (double)MapperExtensions.RoundLon(suggestion.Longitude.Value);

                suggestion.DateSuggestion = MapperExtensions.TruncateToSecond(suggestion.DateSuggestion);

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