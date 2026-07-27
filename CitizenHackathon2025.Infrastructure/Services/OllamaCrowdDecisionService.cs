using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class OllamaCrowdDecisionService : ILocalCrowdDecisionService
    {
        private readonly HttpClient _http;

        public OllamaCrowdDecisionService(HttpClient http)
        {
            _http = http;
        }

        public async Task<LocalCrowdDecisionResult> AnalyzeAsync(LocalCrowdDecisionRequest request, CancellationToken ct = default)
        {
            var systemPrompt = """
                            You are OutZen’s local decision-support engine.

                            You only analyze aggregated crowd concentration data.
                            You must never identify an individual.
                            You must never infer age, name, religion, political opinions,
                            origin, health, or any personal characteristics.
                            You should only help decide whether to avoid an area,
                            display a warning, suggest an alternative, or request human validation  .

                            Respond only in valid JSON.
                            """;

            var userPayload = JsonSerializer.Serialize(request);

            var body = new
            {
                model = "mistral",
                stream = false,
                prompt = systemPrompt + "\n\nOutZen aggregated data:\n" + userPayload + "\n\nReturn a JSON with priority, summary, userMessage, actions, privacyNote."
            };

            using var response = await _http.PostAsync("api/generate", JsonContent.Create(body), ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                ct);

            var text = json?.Response ?? "";

            return JsonSerializer.Deserialize<LocalCrowdDecisionResult>(
                       text,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new LocalCrowdDecisionResult
                   {
                       Priority = "Unknown",
                       Summary = "Local analysis unavailable.",
                       UserMessage = "The situation requires verification.",
                       Actions = new List<string> { "NotifyModerator" },
                       PrivacyNote = "No personal data used."
                   };
        }

        private sealed class OllamaGenerateResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }
    }
}













































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.