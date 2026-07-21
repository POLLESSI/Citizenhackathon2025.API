using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
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

            var numPredict = Math.Clamp(_config.GetValue<int?>("MistralAI:NumPredict") ?? 320, 128, 768);

            var numContext = Math.Clamp(
                _config.GetValue<int?>("MistralAI:NumContext") ?? 4096,
                2048,
                8192);

            var requestBody = BuildChatRequest(
                groundedPrompt: groundedPrompt,
                model: model,
                temperature: temperature,
                stream: false,
                responseLanguage: responseLanguage,
                languagePromptBuilder: _languagePromptBuilder,
                numPredict: numPredict,
                numContext: numContext);

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

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[OLLAMA][SYNC] Non-success response from Ollama. StatusCode={StatusCode}, BodyPreview={BodyPreview}",
                    (int)response.StatusCode,
                    Truncate(rawResponse, 500));
            }

            response.EnsureSuccessStatusCode();

            OllamaChatStreamResponse? parsedResponse;

            try
            {
                parsedResponse = JsonSerializer.Deserialize<OllamaChatStreamResponse>(rawResponse, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[OLLAMA][SYNC] Failed to deserialize Ollama response. " + "BodyPreview={BodyPreview}", Truncate(rawResponse, 1000));

                throw;
            }

            var finalText = NormalizeGeneratedText(parsedResponse?.Message?.Content);

            if (string.IsNullOrWhiteSpace(finalText))
            {
                _logger.LogWarning("[OLLAMA][SYNC] Empty assistant content returned. ElapsedMs={ElapsedMs}", stopwatch.ElapsedMilliseconds);

                return "No response from Mistral.";
            }

            _logger.LogInformation("[OLLAMA][SYNC] Request completed. FinalLength={FinalLength}, ElapsedMs={ElapsedMs}", finalText.Length, stopwatch.ElapsedMilliseconds);

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

            var numPredict = Math.Clamp(_config.GetValue<int?>("MistralAI:NumPredict") ?? 320, 128, 768);

            var numContext = Math.Clamp(_config.GetValue<int?>("MistralAI:NumContext") ?? 4096, 2048, 8192);

            var requestBody = BuildChatRequest(
                groundedPrompt: groundedPrompt,
                model: model,
                temperature: temperature,
                stream: true,
                responseLanguage: responseLanguage,
                languagePromptBuilder: _languagePromptBuilder,
                numPredict: numPredict,
                numContext: numContext);

            _logger.LogInformation("[OLLAMA][STREAM] Request started. BaseAddress={BaseAddress}, Endpoint={Endpoint}, Model={Model}, Temperature={Temperature}, PromptLength={PromptLength}",
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

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

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

            string? doneReason = null;
            int? evalCount = null;
            int? promptEvalCount = null;
            
            long? totalDurationNanoseconds = null;
            long? promptEvalDurationNanoseconds = null;
            long? evalDurationNanoseconds = null;

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

                if (!string.IsNullOrEmpty(chunkText))
                {
                    chunkCount++;

                    // Never delete or add a space here.
                    // The content must be preserved exactly as Ollama sends it.
                    accumulated.Append(chunkText);
                    streamBuffer.Append(chunkText);

                    var shouldFlush =
                        streamBuffer.Length >= 32 ||
                        chunkText.Contains('.') ||
                        chunkText.Contains('!') ||
                        chunkText.Contains('?') ||
                        chunkText.Contains('\n');

                    if (shouldFlush)
                    {
                        var bufferedChunk =
                            streamBuffer.ToString();

                        await onChunk(bufferedChunk);

                        streamBuffer.Clear();
                    }
                }

                if (envelope.Done)
                {
                    doneReason =
                        envelope.DoneReason;

                    evalCount =
                        envelope.EvalCount;

                    promptEvalCount =
                        envelope.PromptEvalCount;

                    totalDurationNanoseconds =
                        envelope.TotalDuration;

                    promptEvalDurationNanoseconds =
                        envelope.PromptEvalDuration;

                    evalDurationNanoseconds =
                        envelope.EvalDuration;

                    _logger.LogWarning(
                        "[OLLAMA][STREAM] Completion received. " +
                        "DoneReason={DoneReason}; EvalCount={EvalCount}; " +
                        "PromptEvalCount={PromptEvalCount}; " +
                        "ChunkCount={ChunkCount}; TotalLength={TotalLength}; " +
                        "ElapsedMs={ElapsedMs}",
                        doneReason,
                        evalCount,
                        promptEvalCount,
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

            var finalText = NormalizeGeneratedText(accumulated.ToString());

            if (string.IsNullOrWhiteSpace(finalText))
            {
                _logger.LogWarning(
                    "[OLLAMA][STREAM] Empty final content returned. " +
                    "ChunkCount={ChunkCount}; LineCount={LineCount}; " +
                    "ElapsedMs={ElapsedMs}",
                    chunkCount,
                    lineCount,
                    stopwatch.ElapsedMilliseconds);

                return "No response from Mistral.";
            }

            var wasLimitedByLength = string.Equals(doneReason, "length", StringComparison.OrdinalIgnoreCase);

            if (wasLimitedByLength)
            {
                _logger.LogWarning(
                    "[OLLAMA][STREAM] Output truncated by num_predict. " +
                    "FinalLength={FinalLength}; EvalCount={EvalCount}; " +
                    "NumPredict={NumPredict}",
                    finalText.Length,
                    evalCount,
                    numPredict);

                // Honestly report an interruption without making up an ending.
                finalText = EnsureEllipsis(finalText);
            }
            else
            {
                // A normally completed response receives at a minimum
                // a terminal punctuation mark.
                finalText = EnsureTerminalPunctuation(finalText);
            }

            var totalSeconds = NanosecondsToSeconds(totalDurationNanoseconds);

            var promptEvalSeconds = NanosecondsToSeconds(promptEvalDurationNanoseconds);

            var evalSeconds = NanosecondsToSeconds(evalDurationNanoseconds);

            double? tokensPerSecond = evalCount.HasValue && evalSeconds.HasValue && evalSeconds.Value > 0d ? evalCount.Value / evalSeconds.Value : null;

            _logger.LogInformation(
                "[OLLAMA][STREAM] Request completed. " +
                "DoneReason={DoneReason}; " +
                "EvalCount={EvalCount}; PromptEvalCount={PromptEvalCount}; " +
                "ChunkCount={ChunkCount}; LineCount={LineCount}; " +
                "FinalLength={FinalLength}; ElapsedMs={ElapsedMs}; " +
                "TotalSeconds={TotalSeconds}; " +
                "PromptEvalSeconds={PromptEvalSeconds}; " +
                "EvalSeconds={EvalSeconds}; " +
                "TokensPerSecond={TokensPerSecond}",
                doneReason,
                evalCount,
                promptEvalCount,
                chunkCount,
                lineCount,
                finalText.Length,
                stopwatch.ElapsedMilliseconds,
                totalSeconds,
                promptEvalSeconds,
                evalSeconds,
                tokensPerSecond);

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

        private static object BuildChatRequest(string groundedPrompt, string model, float temperature, bool stream, string responseLanguage, ILanguagePromptBuilder languagePromptBuilder, int numPredict, int numContext)
        {
            var languageInstruction = languagePromptBuilder.BuildLanguageInstruction(responseLanguage);

            var systemPrompt = $"""
                            You are OutZen, a Belgian intelligent local assistant.
                            You are reliable, factual and concise.
                            Never invent information absent from the supplied context.

                            {languageInstruction}

                            For tourism questions:
                            - return at most 5 recommendations in total
                            - write one numbered recommendation per line
                            - use exactly this format: "1. Name — distance — short factual description."
                            - use exactly one ordinary space after each list number
                            - use exactly one ordinary space around each dash
                            - never concatenate two words
                            - never concatenate a value with the next list number
                            - never output internal backend field names such as: crowd, capacity, advice, distanceKm, tag
                            - omit unavailable fields instead of writing "—"
                            - prioritize events occurring within the requested date range
                            - prefer concrete attractions over generic towns or villages
                            - use only the supplied distances
                            - do not create a second list
                            - finish every numbered item with punctuation
                            - place "Bonne découverte." on a separate final line

                            If the supplied context is insufficient, state it clearly.

                            Strict factual candidate rules:
                            - Recommend only places and events explicitly present in the supplied context.
                            - Never add a place from general model knowledge.
                            - Copy every place and event name exactly as supplied.
                            - Copy every distance exactly as supplied.
                            - If only two valid candidates exist, return only two.
                            - Never complete the list with unsupported candidates.
                            - Do not mention any town, museum, abbey, park or attraction absent from the context.
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
                        content = finalUserPrompt
                    }
                },
                stream,
                keep_alive = "30m",
                options = new
                {
                    temperature,
                    num_predict = numPredict,
                    num_ctx = numContext
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

        private static string EnsureTerminalPunctuation(string text)
        {
            var value = text?.Trim() ?? string.Empty;

            if (value.Length == 0)
                return value;

            var lastCharacter = value[^1];

            if (lastCharacter is
                '.' or
                '!' or
                '?' or
                ':' or
                ';' or
                '…')
            {
                return value;
            }

            return value + ".";
        }

        private static string EnsureEllipsis(string text)
        {
            var value = text?.TrimEnd() ?? string.Empty;

            if (value.Length == 0)
                return value;

            return value.EndsWith("…", StringComparison.Ordinal) ? value : value.TrimEnd('.', ',', ';', ':') + "…";
        }

        private static double? NanosecondsToSeconds(long? nanoseconds)
        {
            if (!nanoseconds.HasValue || nanoseconds.Value <= 0)
            {
                return null;
            }

            return nanoseconds.Value / 1_000_000_000d;
        }
        private static string NormalizeGeneratedText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var value = text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Trim();

            // Several horizontal spaces become a single space.
            // Line breaks are preserved.
            value = Regex.Replace(value, @"[ \t]{2,}", " ");

            // Remove only the spaces around a line break.
            // of a line break.
            value = Regex.Replace(value, @"[ \t]*\n[ \t]*", "\n");

            // Separate the numbered items attached to the text:
            // "extérieur3. Parc" becomes:
            // "extérieur\n3. Parc"
            //
            // The condition (?=\p{L}) prevents confusing
            // a list number with a decimal number like 2.25.
            value = Regex.Replace(value, @"(?<!^)(?<!\n)(?<!\d)([1-9]\.)\s*(?=\p{L})", Environment.NewLine + "$1 ");

            // Place the conclusion on its own line.
            value = Regex.Replace(value, @"(?<!^)(?<!\n)[ \t]*(Enjoy discovering it.\.)$", Environment.NewLine + "$1", RegexOptions.IgnoreCase);

            return value.Trim();
        }
    }
}




































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.