using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
public sealed class OpenAIGptExternalService : IGptExternalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIGptExternalService> _logger;

    public OpenAIGptExternalService(HttpClient httpClient, ILogger<OpenAIGptExternalService> logger)
        => (_httpClient, _logger) = (httpClient, logger);
    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { prompt }),
                Encoding.UTF8,
                "application/json")
        };

        using var resp = await _httpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public Task<string> RefineSuggestionAsync(string raw, CancellationToken ct = default)
        => CompleteAsync(raw, ct);
}






























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.