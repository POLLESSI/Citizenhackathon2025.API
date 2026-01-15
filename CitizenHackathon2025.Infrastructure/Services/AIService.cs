using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly IOpenWeatherService _weather;
    private readonly IAIRepository _aiRepository;

    public AIService(HttpClient httpClient, IOptions<OpenAIOptions> options, IOpenWeatherService weather, IAIRepository aiRepository)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _weather = weather;
        _aiRepository = aiRepository;

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

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_options.ApiUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"OpenAI error {response.StatusCode}: {responseString}");

        using var doc = JsonDocument.Parse(responseString);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response generated.";
    }

    public Task<string> GetSuggestionsAsync(object content)
    {
        var userPrompt = $"You are a tour assistant. Analyze this data:\n{JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true })}";
        return SendChatRequestAsync("You are a smart tourist assistant.", userPrompt);
    }

    public Task<string> GetTouristicSuggestionsAsync(string prompt)
        => SendChatRequestAsync("You are a smart tour assistant.", prompt, 0.2);

    public Task<string> SummarizeTextAsync(string input)
        => SendChatRequestAsync("You are a professional resume assistant.", $"Summarize this in French:\n\n{input}", 0.5);

    public Task<string> GenerateSuggestionAsync(string prompt)
        => SendChatRequestAsync("You are a smart tour assistant.", prompt);

    public Task<string> TranslateToFrenchAsync(string englishText)
        => SendChatRequestAsync("You are a professional translator.", $"Translate into French (natural and professional style):\n\n{englishText}", 0.3);

    public Task<string> TranslateToDutchAsync(string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            throw new ArgumentException("Text cannot be empty.", nameof(englishText));

        return SendChatRequestAsync("You are an English-Dutch translator.", $"Translate into Dutch (natural style):\n\n{englishText}", 0.3);
    }

    public Task<string> TranslateToGermanAsync(string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            throw new ArgumentException("Text cannot be empty", nameof(englishText));

        return SendChatRequestAsync("You are a helpful assistant that translates English to German.", $"Translate the following to German:\n\n{englishText}", 0.3);
    }

    public Task<string> AskChatGptAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));

        return SendChatRequestAsync("You are a helpful assistant answering general questions.", prompt, 0.5);
    }

    public Task<string> SuggestAlternativeAsync(string prompt)
        => Task.FromResult($"Suggestion for: {prompt}");

    public async Task<string> SuggestAlternativeWithWeatherAsync(string location)
    {
        var weatherInfo = await _weather.GetWeatherSummaryAsync(location);
        var prompt = $"Offer an activity to do in {location} given this weather: {weatherInfo}";
        return await SendChatRequestAsync("You are a smart tour assistant.", prompt);
    }

    public async Task<GPTInteraction?> GetChatGptByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        return await _aiRepository.GetByIdAsync(id);
    }

    public async Task SaveInteractionAsync(string prompt, string reply, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(reply))
            throw new ArgumentException("Prompt and reply are required.");

        var interaction = new GPTInteraction { Prompt = prompt, Response = reply, CreatedAt = createdAt, Active = true };
        await _aiRepository.SaveInteractionAsync(interaction);
    }

    public async Task<GPTInteraction> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        var result = await _aiRepository.GetByIdAsync(id);
        return result ?? throw new KeyNotFoundException($"No GPT interaction found with ID {id}");
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.