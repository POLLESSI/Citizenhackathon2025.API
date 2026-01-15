using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenAI;

public sealed class OpenAIGptExternalService : IGptExternalService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAIGptExternalService> _log;

    public OpenAIGptExternalService(HttpClient http, ILogger<OpenAIGptExternalService> log)
        => (_http, _log) = (http, log);

    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        => CallAsync(prompt, ct);

    public Task<string> RefineSuggestionAsync(string raw, CancellationToken ct = default)
        => CallAsync($"Refine: {raw}", ct);

    private async Task<string> CallAsync(string prompt, CancellationToken ct)
    {
        // ⛔ You're not connected => you can throw to test the fallbacks
        // throw new InvalidOperationException("OpenAI not configured.");

        // Or a simple fake:
        return $"[FAKE GPT] {prompt}";
    }
}












































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.