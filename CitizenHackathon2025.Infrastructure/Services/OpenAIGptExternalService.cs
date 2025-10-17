using CitizenHackathon2025.Shared.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class OpenAIGptExternalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIGptExternalService> _logger;
    private readonly AsyncPolicyWrap<HttpResponseMessage> _openAiPolicy;

    public OpenAIGptExternalService(HttpClient httpClient, ILogger<OpenAIGptExternalService> logger, AsyncPolicyWrap<HttpResponseMessage> openAiPolicy)
    {
        _httpClient = httpClient;
        _logger = logger;
        _openAiPolicy = openAiPolicy;
    }

    public async Task<string> AskChatGptAsync(string prompt, string userId, CancellationToken ct = default)
    {
        var ctx = new Context
        {
            ["service"] = "api.openai.com",
            ["operation"] = "POST /v1/chat/completions",
            ["userId"] = userId ?? "anon",
            ["correlationId"] = Guid.NewGuid().ToString("N")
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent($"{{\"prompt\":\"{prompt}\"}}", System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _openAiPolicy.ExecuteAsync(
            (_, token) => _httpClient.SendAsync(request, token),
            ctx,
            ct);

        return await response.Content.ReadAsStringAsync();
    }
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.