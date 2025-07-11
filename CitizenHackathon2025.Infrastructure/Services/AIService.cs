using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(AIService))]

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class AIService : IAIService
    {
        private const string OpenAIBaseUrl = "https://api.openai.com/v1/";
        private const string OpenAIChatEndpoint = "chat/completions";
        private const string OpenAIImageEndpoint = "images/generations";
        private const string OpenAIKeyHeader = "Authorization";
        private const string OpenAIKeyHeaderValue = "Bearer {0}";
        private const string OpenAIContentTypeHeader = "Content-Type";
        private const string OpenAIContentTypeHeaderValue = "application/json";
        private const string OpenAIModelKey = "model";
        private const string OpenAIMessagesKey = "messages";
        private const string OpenAIImagePromptKey = "prompt";
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly IOpenWeatherService _weather;
        private readonly string _apiKey = "sk-xxxxxxx"; // OpenAI key preferably, injected via config
        private readonly string _model;
        private readonly IConfiguration _config;
        private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        private readonly IGptInteractionRepository _gptInteractionRepository;

        public AIService(HttpClient httpClient, IOptions<OpenAIOptions> options, IOpenWeatherService weather, IGptInteractionRepository gptInteractionRepository)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _weather = weather;
            _gptInteractionRepository = gptInteractionRepository;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        public string GetModel() => _options.Model;
        private async Task<string> SendChatRequestAsync(string systemPrompt, string userPrompt, double temperature = 0.7)
        {
            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
                temperature
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.ApiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI error {response.StatusCode}: {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString() ?? "Aucune réponse générée.";
        }

        public Task<string> GetSuggestionsAsync(object content)
        {
            var userPrompt = $"You are a tour assistant. Here is some data to analyze :\n{System.Text.Json.JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true })}";
            return SendChatRequestAsync("You are a smart tourist assistant.", userPrompt);
        }

        public Task<string> GetTouristicSuggestionsAsync(string prompt)
        {
            return SendChatRequestAsync("You are a smart tour assistant.", prompt, temperature: 0.2);
        }

        public Task<string> SummarizeTextAsync(string input)
        {
            var prompt = $"Summarize the following text clearly and concisely, in French :\n\n{input}";
            return SendChatRequestAsync("You are a professional resume assistant.", prompt, temperature: 0.5);
        }

        public Task<string> GenerateSuggestionAsync(string prompt)
        {
            return SendChatRequestAsync("You are a smart tour assistant.", prompt);
        }

        public Task<string> TranslateToFrenchAsync(string englishText)
        {
            var prompt = $"Translate into French (natural and professional style) :\n\n{englishText}";
            return SendChatRequestAsync("You are a professional translator.", prompt, temperature: 0.3);
        }

        public Task<string> TranslateToDutchAsync(string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                throw new ArgumentException("Text cannot be empty.", nameof(englishText));

            var prompt = $"Translate into Dutch (natural style) :\n\n{englishText}";
            return SendChatRequestAsync("You are an English-Dutch translator.", prompt, temperature: 0.3);
       
            //await Task.Delay(1); // Just to keep it async
            //return "Mock translation into Dutch.";
        }

        public async Task<string> TranslateToGermanAsync(string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                throw new ArgumentException("Text cannot be empty", nameof(englishText));

            var prompt = $"Translate the following English text to German (Deutsch):\n\n{englishText}";

            var requestBody = new
            {
                model = "gpt-4", // or "gpt-3.5-turbo" depending on subscription
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that translates English to German." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var requestJson = System.Text.Json.JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI error: {response.StatusCode} - {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var translation = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return translation ?? "No translation generated.";
        }

        public async Task<string> SuggestAlternativeAsync(string prompt)
        {
            // Fictitious example — adapt according to your GPT, OpenAI, or other engine.
            return await Task.FromResult($"Suggestion for : {prompt}");
        }

        public async Task<string> SuggestAlternativeWithWeatherAsync(string location)
        {
            var weatherInfo = await _weather.GetWeatherSummaryAsync(location);

            string prompt = $"Offers a pleasant activity to do at {location} with the following weather : {weatherInfo}";

            // Dummy call to GPT (replace with your call to OpenAI or other)
            return $"[Suggestion AI] HAS {location}, {weatherInfo}, You could: visit a museum, go to the cinema, or explore a covered gallery.";
        }

        public async Task<string> AskChatGptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));

            try
            {
                // Construction of the system prompt
                string systemPrompt = "You are a helpful assistant answering general purpose user questions.";

                // Call to the OpenAI API
                var requestBody = new
                {
                    model = _options.Model, // or "gpt-4", "gpt-3.5-turbo" depending on your config
                    messages = new[]
                    {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            },
                    temperature = 0.5
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_options.ApiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenAI API Error: {response.StatusCode} - {responseString}");
                }

                using var doc = JsonDocument.Parse(responseString);
                var reply = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString() ?? "No response.";

                // Backup to database via repository
                if (_gptInteractionRepository is not null)
                {
                    await _gptInteractionRepository.SaveInteractionAsync(prompt, reply, DateTime.UtcNow);
                }

                return reply;
            }
            catch (Exception ex)
            {
                // Logging potentially to be added here
                return $"Erreur lors de l'appel à GPT : {ex.Message}";
            }
        }

        public async Task<GPTInteraction?> GetChatGptByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "The identifier must be strictly positive.");

            try
            {
                var interaction = await _gptInteractionRepository.GetByIdAsync(id);
                if (interaction == null)
                {
                    return null;
                }

                return interaction;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error retrieving GPT interaction with ID {id}.", ex);
            }
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.