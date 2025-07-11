using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> AskChatGptAsync(string prompt)
        {
            return await Task.FromResult($"[GPT MOCKED RESPONSE] => {prompt}");
        }

        public async Task<string> GenerateSuggestionAsync(string prompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is not configured.");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = "You are a smart travel assistant who suggests relevant alternatives depending on the weather and crowds." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI API error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var suggestion = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return suggestion ?? "Suggestion not available.";
        }

        public Task<GPTInteraction?> GetChatGptByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSuggestionsAsync(object content)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTouristicSuggestionsAsync(string prompt)
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

        public Task<string> SummarizeTextAsync(string input)
        {
            throw new NotImplementedException();
        }

        public Task<string> TranslateToDutchAsync(string englishText)
        {
            throw new NotImplementedException();
        }

        public Task<string> TranslateToFrenchAsync(string englishText)
        {
            throw new NotImplementedException();
        }

        public Task<string> TranslateToGermanAsync(string englishText)
        {
            throw new NotImplementedException();
        }
    }
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.