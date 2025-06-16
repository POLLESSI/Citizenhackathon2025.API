using Citizenhackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Application.Services
{
    public class ChatGptService : IAIService
    {
    #nullable disable
        private readonly HttpClient _httpClient;
        private readonly IGptInteractionRepository _repository;
        private readonly ILogger<ChatGptService> _logger;
        private readonly string _apiKey;

        public ChatGptService(IOptions<OpenAIOptions> options, HttpClient httpClient, IGptInteractionRepository repository, ILogger<ChatGptService> logger)
        {
            var apiKey = options.Value.ApiKey;
            _httpClient = httpClient;
            _repository = repository;
            _logger = logger;
            _apiKey = apiKey;
        }

        private async Task<string> SendPromptAsync(string systemMessage, string userInput)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userInput }
                },
                temperature = 0.2
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            HttpResponseMessage response = null; 

            try
            {
                response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

            }
            catch (BrokenCircuitException ex)
            {

                _logger.LogError("Open Circuit Breaker: {Message}", ex.Message);
                return "Service temporarily unavailable. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error calling OpenAI API: {Message}", ex.Message);
                return "An error occurred while communicating with the AI ​​service.";
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            await _repository.SaveInteractionAsync(userInput, content, DateTime.Now);

            return content;
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
    }
}

