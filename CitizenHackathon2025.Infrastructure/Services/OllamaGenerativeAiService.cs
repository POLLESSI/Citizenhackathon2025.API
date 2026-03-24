using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class OllamaGenerativeAiService : IGenerativeAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OllamaGenerativeAiService> _logger;

        public OllamaGenerativeAiService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<OllamaGenerativeAiService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default)
        {
            var model = _config["MistralAI:Model"] ?? "mistral";
            var temperature = _config.GetValue<float?>("MistralAI:Temperature") ?? 0.3f;

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
                    temperature
                }
            };

            using var response = await _httpClient.PostAsJsonAsync("api/chat", request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Ollama status code: {StatusCode}", (int)response.StatusCode);

            response.EnsureSuccessStatusCode();

            var json = JsonSerializer.Deserialize<MistralResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return json?.Message?.Content?.Trim() ?? "No response from Ollama.";
        }
    }
}