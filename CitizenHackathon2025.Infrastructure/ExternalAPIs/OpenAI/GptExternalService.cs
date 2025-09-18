using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Shared.Json;
using System.Net.Http.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI
{
    public class GptExternalService : IGptExternalService
    {
        private readonly HttpClient _http;

        public GptExternalService(HttpClient http) => _http = http;

        public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            var req = new GptRequest
            {
                Messages = new[] { new GptMessage { Role = "user", Content = prompt } }
            };

            using var resp = await _http.PostAsJsonAsync(
                "/v1/chat/completions",
                req,
                JsonDefaults.Options,
                ct);

            resp.EnsureSuccessStatusCode();

            var data = await resp.Content.ReadFromJsonAsync<GptResponse>(JsonDefaults.Options, ct)
                       ?? throw new InvalidOperationException("Empty GPT response");

            return data.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
        }

        public async Task<string> RefineSuggestionAsync(string raw, CancellationToken ct = default)
        {
            var prompt =
                "You are an assistant who reformulates and improves suggestions for a French-speaking audience, " +
                "concise, concrete style, with bullet points where useful. Keeps the meaning, removes the verbiage.\n\n" +
                "Suggestion for improvement :\n" + raw;

            return await CompleteAsync(prompt, ct);
        }
    }
}