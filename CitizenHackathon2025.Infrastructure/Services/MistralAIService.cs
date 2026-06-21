using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class MistralAIService : IMistralAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<MistralAIService> _logger;
        private readonly ILanguagePromptBuilder _languagePromptBuilder;
        private static readonly Uri OllamaChatEndpoint = new("api/chat", UriKind.Relative);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MistralAIService(HttpClient httpClient, IConfiguration config, ILogger<MistralAIService> logger, ILanguagePromptBuilder languagePromptBuilder)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languagePromptBuilder = languagePromptBuilder ?? throw new ArgumentNullException(nameof(languagePromptBuilder));

            _logger.LogWarning("[MISTRAL DI CHECK] BaseAddress={BaseAddress}, Timeout={Timeout}", _httpClient.BaseAddress, _httpClient.Timeout);
        }
        public async Task<string> GenerateFromPromptAsync(string groundedPrompt, string responseLanguage = "fr-FR", CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(groundedPrompt))
                throw new ArgumentException("Grounded prompt cannot be null or empty.", nameof(groundedPrompt));

            _logger.LogInformation("[OLLAMA] PromptLength={Length}", groundedPrompt.Length);

            _logger.LogDebug("[OLLAMA] PromptPreview={Preview}", groundedPrompt.Length > 1000 ? groundedPrompt[..1000] : groundedPrompt);

            var stopwatch = Stopwatch.StartNew();
            var model = GetModel();
            var temperature = GetTemperature();

            var requestBody = BuildChatRequest(
                groundedPrompt: groundedPrompt,
                model: model,
                temperature: temperature,
                stream: false,
                responseLanguage: responseLanguage,
                languagePromptBuilder: _languagePromptBuilder);

            _logger.LogInformation(
                "[OLLAMA][SYNC] Request started. BaseAddress={BaseAddress}, Endpoint={Endpoint}, Model={Model}, Temperature={Temperature}, PromptLength={PromptLength}",
                _httpClient.BaseAddress?.ToString() ?? "<null>",
                OllamaChatEndpoint,
                model,
                temperature,
                groundedPrompt.Length);

            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaChatEndpoint)
            {
                Content = JsonContent.Create(requestBody, options: JsonOptions)
            };

            using var response = await _httpClient.SendAsync(request, ct);
            var rawResponse = await response.Content.ReadAsStringAsync(ct);

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                _logger.LogWarning("[OLLAMA][SYNC] Empty HTTP body returned.");
                return "No response from Mistral.";
            }

            //var parsedResponse = JsonSerializer.Deserialize<MistralResponse>(rawResponse, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[OLLAMA][SYNC] Non-success response from Ollama. StatusCode={StatusCode}, BodyPreview={BodyPreview}",
                    (int)response.StatusCode,
                    Truncate(rawResponse, 500));
            }

            response.EnsureSuccessStatusCode();

            MistralResponse? parsedResponse;
            try
            {
                parsedResponse = JsonSerializer.Deserialize<MistralResponse>(rawResponse, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "[OLLAMA][SYNC] Failed to deserialize Ollama response. BodyPreview={BodyPreview}",
                    Truncate(rawResponse, 1000));
                throw;
            }

            var finalText = parsedResponse?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(finalText))
            {
                _logger.LogWarning(
                    "[OLLAMA][SYNC] Empty assistant content returned. ElapsedMs={ElapsedMs}",
                    stopwatch.ElapsedMilliseconds);

                return "No response from Mistral.";
            }

            _logger.LogInformation(
                "[OLLAMA][SYNC] Request completed. FinalLength={FinalLength}, ElapsedMs={ElapsedMs}",
                finalText.Length,
                stopwatch.ElapsedMilliseconds);

            return finalText;
        }

        public async Task<string> StreamFromPromptAsync(string groundedPrompt, Func<string, Task> onChunk, string responseLanguage = "fr-FR", CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(onChunk);

            if (string.IsNullOrWhiteSpace(groundedPrompt))
                throw new ArgumentException("Grounded prompt cannot be null or empty.", nameof(groundedPrompt));

            var stopwatch = Stopwatch.StartNew();
            var model = GetModel();
            var temperature = GetTemperature();
            //var chatUri = BuildChatUri();

            var requestBody = BuildChatRequest(
                groundedPrompt: groundedPrompt,
                model: model,
                temperature: temperature,
                stream: true,
                responseLanguage: responseLanguage,
                languagePromptBuilder: _languagePromptBuilder);

            _logger.LogInformation(
                "[OLLAMA][STREAM] Request started. BaseAddress={BaseAddress}, Endpoint={Endpoint}, Model={Model}, Temperature={Temperature}, PromptLength={PromptLength}",
                _httpClient.BaseAddress?.ToString() ?? "<null>",
                OllamaChatEndpoint,
                model,
                temperature,
                groundedPrompt.Length);

            var accumulated = new StringBuilder(4096);
            var streamBuffer = new StringBuilder(256);

            var chunkCount = 0;
            var lineCount = 0;

            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaChatEndpoint)
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            _logger.LogInformation(
                "[OLLAMA][STREAM] Response headers received. StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);

                _logger.LogWarning(
                    "[OLLAMA][STREAM] Non-success response from Ollama. StatusCode={StatusCode}, BodyPreview={BodyPreview}",
                    (int)response.StatusCode,
                    Truncate(errorBody, 500));
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(ct);
                lineCount++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                OllamaChatStreamResponse? envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<OllamaChatStreamResponse>(line, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "[OLLAMA][STREAM] Failed to deserialize stream line #{LineCount}. LinePreview={LinePreview}",
                        lineCount,
                        Truncate(line, 500));
                    continue;
                }

                if (envelope is null)
                    continue;

                var chunkText = envelope.Message?.Content ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(chunkText))
                {
                    chunkCount++;

                    accumulated.Append(chunkText);

                    // Streaming buffering
                    streamBuffer.Append(chunkText);

                    // Smart Flush
                    var shouldFlush =
                        streamBuffer.Length >= 32 ||
                        chunkText.Contains('.') ||
                        chunkText.Contains('!') ||
                        chunkText.Contains('?') ||
                        chunkText.Contains('\n');

                    if (shouldFlush)
                    {
                        var bufferedChunk = streamBuffer.ToString();

                        await onChunk(bufferedChunk);

                        streamBuffer.Clear();
                    }
                }

                if (envelope.Done)
                {
                    _logger.LogInformation(
                        "[OLLAMA][STREAM] Stream completion received. DoneReason={DoneReason}, ChunkCount={ChunkCount}, TotalLength={TotalLength}, ElapsedMs={ElapsedMs}",
                        envelope.DoneReason,
                        chunkCount,
                        accumulated.Length,
                        stopwatch.ElapsedMilliseconds);
                    break;
                }
            }

            if (streamBuffer.Length > 0)
            {
                await onChunk(streamBuffer.ToString());

                streamBuffer.Clear();
            }

            var finalText = accumulated.ToString().Trim();

            if (string.IsNullOrWhiteSpace(finalText))
            {
                _logger.LogWarning(
                    "[OLLAMA][STREAM] Empty final content returned. ChunkCount={ChunkCount}, LineCount={LineCount}, ElapsedMs={ElapsedMs}",
                    chunkCount,
                    lineCount,
                    stopwatch.ElapsedMilliseconds);

                return "No response from Mistral.";
            }

            _logger.LogInformation(
                "[OLLAMA][STREAM] Request completed. ChunkCount={ChunkCount}, LineCount={LineCount}, FinalLength={FinalLength}, ElapsedMs={ElapsedMs}",
                chunkCount,
                lineCount,
                finalText.Length,
                stopwatch.ElapsedMilliseconds);

            return finalText;
        }

        public Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<string> CallOllamaApi(string prompt, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<int> ArchivePastGptInteractionsAsync()
            => throw new NotImplementedException();

        private string GetModel()
            => _config["MistralAI:Model"] ?? "mistral";

        private float GetTemperature()
            => _config.GetValue<float?>("MistralAI:Temperature") ?? 0.3f;

        private static object BuildChatRequest(string groundedPrompt, string model, float temperature, bool stream, string responseLanguage, ILanguagePromptBuilder languagePromptBuilder)
        {
            var languageInstruction =
                languagePromptBuilder.BuildLanguageInstruction(responseLanguage);

            var systemPrompt = $"""
                    You are OutZen, Belgian intelligent local assistant.
                    You are reliable, cautious, factual, and you never invent information that is not part of the provided context.

                    {languageInstruction}

                    You help users with:
                    - weather
                    - traffic
                    - events
                    - safety
                    - crowding
                    - local suggestions

                    If the context does not contain enough information, state it clearly.
                    The final response language must follow the final output language instruction.
                    """;
            var finalUserPrompt = $"""
                {groundedPrompt}

                Final output language instruction:
                {languageInstruction}
                """;

            return new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = $"""
                        {groundedPrompt}

                        Final output language instruction:
                        {languageInstruction}
                        """
                    }
                },
                stream,
                options = new
                {
                    temperature
                }
            };
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "<empty>";

            var normalized = value
                .Replace(Environment.NewLine, " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();

            if (normalized.Length <= maxLength)
                return normalized;

            return normalized[..maxLength] + "...";
        }
    }
}




































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.