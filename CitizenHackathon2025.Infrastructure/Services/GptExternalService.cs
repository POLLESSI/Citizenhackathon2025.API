using System.Net.Http.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GptExternalService
    {
        private readonly HttpClient _http;

        public GptExternalService(HttpClient http) => _http = http;

        public async Task<string?> RefineSuggestionAsync(string prompt, CancellationToken ct)
        {
            var request = new
            {
                model = "gpt-4o",
                temperature = 0.8,
                messages = new[] {
                new { role = "system", content = "Tu es un assistant belge expert des sorties calmes en Wallonie." },
                new { role = "user", content = $"Peux-tu améliorer cette suggestion : {prompt}" }
            }
            };

            var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<OpenAIResponse>();

            return json?.choices.FirstOrDefault()?.message.content?.Trim();
        }

        private class OpenAIResponse
        {
            public List<Choice> choices { get; set; } = new();
            public class Choice { public Message message { get; set; } = new(); }
            public class Message { public string content { get; set; } = ""; }
        }
    }
}
