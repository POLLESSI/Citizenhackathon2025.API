using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain;
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

[assembly: OwinStartup(typeof(Citizenhackathon2025.Application.Services.AIService))]

namespace Citizenhackathon2025.Application.Services
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
        private readonly string _apiKey = "sk-xxxxxxx"; // clé OpenAI de préférence, injectée via config
        private readonly string _model;
        private readonly IConfiguration _config;
        private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";

        public AIService(IOptions<OpenAIOptions> options, HttpClient httpClient, string apiKey)
        {
        #nullable disable
            _httpClient = httpClient;
            _options = (OpenAIOptions)options;
            _apiKey = options.Value.ApiKey;
            _model = options.Value.Model;
        }

        public async Task<string> GetSuggestionsAsync(object content)
        {
            // Étape 1 : Transformation de l'objet content en prompt lisible
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string prompt = $"You are a tour assistant. Make suggestions based on this information: \n{jsonContent}";

            // Étape 2 : Création du payload pour OpenAI
            var requestBody = new
            {
                model = "gpt-4", // ou gpt-3.5-turbo selon ton abonnement
                messages = new[]
                {
                new { role = "system", content = "You are a smart tourist assistant." },
                new { role = "user", content = prompt }
            },
                temperature = 0.7
            };

            var requestJson = System.Text.Json.JsonSerializer.Serialize(requestBody);

            // Étape 3 : Configuration de la requête HTTP
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Étape 4 : Appel API OpenAI
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI error: {response.StatusCode} - {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result ?? "No suggestions generated.";
        }

        public async Task<string> GetTouristicSuggestionsAsync(string prompt)
        {
        #nullable disable
            var request = new
            {
                model = "gpt-4o", // or gpt-4o-mini if ​​you have access to it
                messages = new[]
                {
                new { role = "system", content = "You are a smart tourist assistant." },
                new { role = "user", content = prompt }
            },
                temperature = 0.2
            };

            var req = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
            req.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json);

            return result.choices[0].message.content.ToString();
        }

        public async Task<string> SummarizeTextAsync(string input)
        {
            var prompt = $"Summarize the following text clearly and concisely in French :\n\n{input}";

            var requestBody = new
            {
                model = "gpt-4", // ou "gpt-3.5-turbo" si tu n’as pas accès à GPT-4
                messages = new[]
                {
            new { role = "system", content = "You are an assistant who summarizes texts." },
            new { role = "user", content = prompt }
        },
                temperature = 0.5
            };

            var requestJson = System.Text.Json.JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"Summary error with OpenAI: {response.StatusCode} - {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString()
                      ?? "Summary not generated.";
        }

        public async Task<string> TranslateToFrenchAsync(string englishText)
        {
            var prompt = $"Translate the following text into French, keeping a natural and professional style :\n\n{englishText}";

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a professional translator." },
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
                throw new Exception($"Translation error with OpenAI : {response.StatusCode} - {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString()
                      ?? "Translation not generated.";
        }
        public async Task<string> TranslateToDutchAsync(string englishText)
        {
            if (string.IsNullOrWhiteSpace(englishText))
                throw new ArgumentException("Text cannot be empty", nameof(englishText));

            var prompt = $"Translate the following English text to Dutch (Nederlands):\n\n{englishText}";

            var requestBody = new
            {
                model = "gpt-4", 
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that translates English to Dutch." },
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

            //await Task.Delay(1); // Juste pour garder async
            //return "Traduction simulée en néerlandais.";
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
    }
}
