using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.Observability;
using CitizenHackathon2025.Shared.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class ChatGptService : IAIService
    {
#nullable disable
        private readonly HttpClient _httpClient;
        private readonly IGptInteractionRepository _repo;
        private readonly ILogger<ChatGptService> _logger;
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline; // v8
        private readonly string _apiKey;

        public ChatGptService(
            IOptions<OpenAIOptions> options,
            HttpClient httpClient,
            IGptInteractionRepository repo,
            ILogger<ChatGptService> logger,
            /* inject pipelines holder */ dynamic pipelines)
        {
            _httpClient = httpClient;
            _repo = repo;
            _logger = logger;
            _apiKey = options.Value.ApiKey;
            _pipeline = pipelines.OpenAi; // "openai" pipeline
        }

        private async Task<string> SendPromptAsync(string systemMessage, string userInput)
        {
            var body = new
            {
                model = "gpt-4o",
                messages = new[] { new { role = "system", content = systemMessage }, new { role = "user", content = userInput } },
                temperature = 0.2
            };
            var req = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var (ctx, corr) = ObservabilityContext.Create(
                service: _httpClient.BaseAddress?.Host ?? "api.openai.com",
                operation: "POST /v1/chat/completions",
                userId: Thread.CurrentPrincipal?.Identity?.Name);

            HttpResponseMessage resp;
            try
            {
                resp = await Resilience.ExecuteLoggedAsync(
                    _pipeline,
                    ct => new ValueTask<HttpResponseMessage>(_httpClient.SendAsync(req, ct)), // <- wrap Task => ValueTask
                    ctx,
                    _logger,
                    "openai");
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "OpenAI circuit open corr={CorrelationId}", corr);
                await _repo.SaveInteractionAsync(userInput, "CIRCUIT_OPEN", DateTime.UtcNow);
                _logger.LogWarning("GPT meta {@Meta}", new { correlationId = corr, policy = "openai", error = "circuit_open" });
                return "Service temporarily unavailable. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI call error corr={CorrelationId}", corr);
                await _repo.SaveInteractionAsync(userInput, "ERROR", DateTime.UtcNow);
                _logger.LogError(ex, "GPT meta {@Meta}", new { correlationId = corr, policy = "openai", error = ex.GetType().Name });
                return "An error occurred while communicating with the AI service.";
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            await _repo.SaveInteractionAsync(userInput, content, DateTime.UtcNow);
            _logger.LogInformation("GPT meta {@Meta}", new
            {
                correlationId = corr,
                policy = "openai",
                status = (int)resp.StatusCode,
                promptChars = userInput?.Length ?? 0,
                responseChars = content?.Length ?? 0
            });

            return content ?? "";
        }


        public async Task<string> AskChatGptAsync(string userInput)
        {
            return await SendPromptAsync("You are a helpful assistant in tourist orientation.", userInput);
        }
        Task<string> IAIService.GetSuggestionsAsync(object content)
        {
            throw new NotImplementedException();
        }


        public async Task GetSuggestionsAsync(object content)
        {
            var prompt = $"Analyze this content and suggest 3 activity ideas : {JsonSerializer.Serialize(content)}";
            var result = await SendPromptAsync("You are an intelligent assistant who suggests activities based on weather and crowd constraints.", prompt);
            _logger.LogInformation("AI Suggestions :\n" + result);
        }
        public async Task<string> GetTouristicSuggestionsAsync(string prompt)
        {
            return await SendPromptAsync("You're a local tour guide. Suggest specific activities, tailored to the weather and preferences.", prompt);
        }

        public async Task<string> SummarizeTextAsync(string input)
        {
            var prompt = $"Summarize this text in 3 sentences:\n\n{input}";
            return await SendPromptAsync("You are an assistant who summarizes concisely and clearly.", prompt);
        }

        public async Task<string> TranslateToFrenchAsync(string englishText)
        {
            var prompt = $"Translate this text to French: {englishText}";
            return await SendPromptAsync("You are a professional English-to-French translator.", prompt);
        }
        public async Task<string> TranslateToDutchAsync(string englishText)
        {
            var prompt = $"Translate this text to Dutch: {englishText}";
            return await SendPromptAsync("You are a professional English-to-Dutch translator.", prompt);
        }
        public async Task<string> SimulateFailureAsync()
        {
            // Will raise exceptions to test the Retry + CircuitBreaker
            throw new HttpRequestException("Simulated HTTP failure");
        }

        public async Task<string> TranslateToGermanAsync(string englishText)
        {
            var prompt = $"Translate this text to German: {englishText}";
            return await SendPromptAsync("You are a professional English-to-German translator.", prompt);
        }

        public Task<string> GenerateSuggestionAsync(string prompt)
        {
            throw new NotImplementedException();
        }

        public Task<string> SuggestAlternativeAsync(string prompt)
        {
            throw new NotImplementedException();
        }

        public Task<string> SuggestAlternativeWithWeatherAsync(string location)
        {
            throw new NotImplementedException();
        }

        public async Task SaveInteractionAsync(string prompt, string reply, DateTime createdAt)
        {
            await _repo.SaveInteractionAsync(prompt, reply, createdAt);
        }

        public async Task<GPTInteraction> GetChatGptByIdAsync(int id)
        {
            var result = await _repo.GetByIdAsync(id);
            if (result is null)
                throw new KeyNotFoundException($"GPTInteraction #{id} not found.");
            return result;
        }

        public async Task<GPTInteraction> GetByIdAsync(int id)
        {
            var result = await _repo.GetByIdAsync(id);
            if (result is null)
                throw new KeyNotFoundException($"GPTInteraction #{id} not found.");
            return result;
        }

    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.