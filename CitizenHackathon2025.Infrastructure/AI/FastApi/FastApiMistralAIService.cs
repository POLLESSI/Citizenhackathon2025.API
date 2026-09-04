using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.AI.FastApi
{
    /// <summary>
    /// Adapter between the existing OutZen GPT pipeline
    /// and the Python OutZen.AI FastAPI microservice.
    ///
    /// ASP.NET Core
    ///     -> FastApiMistralAIService
    ///     -> FastAPI
    ///     -> Ollama
    ///     -> Mistral
    /// </summary>
    public sealed class FastApiMistralAIService : IMistralAIService
    {
        private const string InternalApiKeyHeader = "X-OutZen-Internal-Key";

        private readonly HttpClient _httpClient;
        private readonly FastApiAiOptions _options;
        private readonly ILogger<FastApiMistralAIService> _logger;
        private readonly ILanguagePromptBuilder _languagePromptBuilder;
        private readonly IGptInteractionRepository _gptInteractionRepository;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public FastApiMistralAIService(HttpClient httpClient, IOptions<FastApiAiOptions> options, ILogger<FastApiMistralAIService> logger, ILanguagePromptBuilder languagePromptBuilder, IGptInteractionRepository gptInteractionRepository)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            ArgumentNullException.ThrowIfNull(options);

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languagePromptBuilder = languagePromptBuilder ?? throw new ArgumentNullException(nameof(languagePromptBuilder));
            _gptInteractionRepository = gptInteractionRepository ?? throw new ArgumentNullException(nameof(gptInteractionRepository));

            _logger.LogInformation(
                "[FASTAPI-AI] Adapter initialized. " +
                "BaseAddress={BaseAddress}; " +
                "Endpoint={Endpoint}; " +
                "TimeoutSeconds={TimeoutSeconds}; " +
                "InternalKeyConfigured={InternalKeyConfigured}",
                _httpClient.BaseAddress?.ToString() ?? "<null>",
                _options.GenerationEndpoint,
                _options.TimeoutSeconds,
                !string.IsNullOrWhiteSpace(_options.InternalApiKey));
        }


        // ============================================================
        // Main generation
        // ============================================================

        public async Task<string> GenerateFromPromptAsync(string groundedPrompt, string responseLanguage = "fr-FR", CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(groundedPrompt))
            {
                throw new ArgumentException("Grounded prompt cannot be null or empty.", nameof(groundedPrompt));
            }

            EnsureConfiguration();

            var language = NormalizeLanguage(responseLanguage);

            /*
             * Preserve the language behaviour that existed in
             * MistralAIService.
             *
             * In particular this is important for:
             * - Russian place names
             * - Arabic
             * - Dutch
             * - German
             * - experimental Walloon
             * - etc.
             */
            var languageInstruction = _languagePromptBuilder.BuildLanguageInstruction(language);
            var effectiveGroundedPrompt = BuildEffectiveGroundedPrompt(groundedPrompt, languageInstruction);
            var requestBody =
                new FastApiGenerationRequest
                {
                    GroundedPrompt = effectiveGroundedPrompt,
                    ResponseLanguage = language,
                    Temperature = _options.DefaultTemperature
                };

            var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 1, 1800);

            using var generationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            generationCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var generationToken = generationCts.Token;
            var stopwatch = Stopwatch.StartNew();
            var endpoint = GetGenerationEndpoint();

            _logger.LogInformation(
                "[FASTAPI-AI][GENERATE] Request starting. " +
                "BaseAddress={BaseAddress}; " +
                "Endpoint={Endpoint}; " +
                "PromptLength={PromptLength}; " +
                "Language={Language}; " +
                "Temperature={Temperature}; " +
                "TimeoutSeconds={TimeoutSeconds}",
                _httpClient.BaseAddress?.ToString() ?? "<null>", endpoint, groundedPrompt.Length, language, _options.DefaultTemperature, timeoutSeconds);

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

                /*
                 * Important:
                 *
                 * Attach the secret ONLY to this internal request.
                 *
                 * Do not put it in logs.
                 * Do not expose it to Blazor.
                 */
                var headerAdded = httpRequest.Headers.TryAddWithoutValidation(InternalApiKeyHeader, _options.InternalApiKey);

                if (!headerAdded)
                {
                    throw new InvalidOperationException($"Unable to add " + $"{InternalApiKeyHeader} header.");
                }

                httpRequest.Content = JsonContent.Create(requestBody, options: JsonOptions);

                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, generationToken).ConfigureAwait(false);

                var rawResponse = await response.Content.ReadAsStringAsync(generationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    LogHttpFailure(response, rawResponse, stopwatch.ElapsedMilliseconds);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new HttpRequestException("OutZen.AI rejected the internal " + "service authentication key.", null, response.StatusCode);
                    }

                    throw new HttpRequestException($"OutZen.AI returned HTTP " + $"{(int)response.StatusCode} " + $"({response.StatusCode}).", null, response.StatusCode);
                }

                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    throw new InvalidOperationException("OutZen.AI returned an empty HTTP body.");
                }

                FastApiGenerationResponse? generationResponse;

                try
                {
                    generationResponse = JsonSerializer.Deserialize<FastApiGenerationResponse>(rawResponse, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "[FASTAPI-AI][GENERATE] " + "Unable to deserialize response. " + "BodyPreview={BodyPreview}", Truncate(rawResponse, 1000));
                    throw new InvalidOperationException("OutZen.AI returned an invalid " + "generation response.", ex);
                }

                if (generationResponse is null)
                {
                    throw new InvalidOperationException("OutZen.AI returned a null " + "generation response.");
                }

                var finalText = NormalizeGeneratedText(generationResponse.Response);

                if (string.IsNullOrWhiteSpace(finalText))
                {
                    throw new InvalidOperationException("OutZen.AI returned an empty " + "AI response.");
                }

                _logger.LogInformation(
                    "[FASTAPI-AI][GENERATE] " +
                    "Request completed. " +
                    "Provider={Provider}; " +
                    "Model={Model}; " +
                    "ResponseLength={ResponseLength}; " +
                    "ElapsedMs={ElapsedMs}",
                    generationResponse.Provider,
                    generationResponse.Model,
                    finalText.Length,
                    stopwatch.ElapsedMilliseconds);

                return finalText;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested && generationCts.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "[FASTAPI-AI][GENERATE] " +
                    "Generation timeout. " +
                    "TimeoutSeconds={TimeoutSeconds}; " +
                    "ElapsedMs={ElapsedMs}",
                    timeoutSeconds,
                    stopwatch.ElapsedMilliseconds);

                throw new TimeoutException($"OutZen.AI generation exceeded " + $"{timeoutSeconds} seconds.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "[FASTAPI-AI][GENERATE] " +
                    "HTTP communication failure. " +
                    "BaseAddress={BaseAddress}; " +
                    "Endpoint={Endpoint}; " +
                    "ElapsedMs={ElapsedMs}",
                    _httpClient.BaseAddress?.ToString() ?? "<null>",
                    endpoint,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }


        // ============================================================
        // Streaming compatibility
        // ============================================================

        public async Task<string> StreamFromPromptAsync(string groundedPrompt, Func<string, Task> onChunk, string responseLanguage = "fr-FR", CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(onChunk);

            if (string.IsNullOrWhiteSpace(groundedPrompt))
            {
                throw new ArgumentException("Grounded prompt cannot be null or empty.", nameof(groundedPrompt));
            }

            EnsureConfiguration();

            var language = NormalizeLanguage(responseLanguage);

            /*
             * Keep the existing OutZen language rules.
             */
            var languageInstruction = _languagePromptBuilder.BuildLanguageInstruction(language);
            var effectiveGroundedPrompt = BuildEffectiveGroundedPrompt(groundedPrompt, languageInstruction);
            var requestBody =
                new FastApiGenerationRequest
                {
                    GroundedPrompt = effectiveGroundedPrompt,
                    ResponseLanguage = language,
                    Temperature = _options.DefaultTemperature
                };

            var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 1, 1800);

            using var generationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            generationCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var generationToken = generationCts.Token;
            var stopwatch = Stopwatch.StartNew();
            var accumulatedResponse = new StringBuilder(capacity: 4096);
            var chunkCount = 0;

            _logger.LogInformation(
                "[FASTAPI-AI][STREAM] Starting. " +
                "BaseAddress={BaseAddress}; " +
                "Endpoint={Endpoint}; " +
                "PromptLength={PromptLength}; " +
                "Language={Language}; " +
                "Temperature={Temperature}; " +
                "TimeoutSeconds={TimeoutSeconds}",
                _httpClient.BaseAddress?.ToString() ?? "<null>",
                _options.StreamingEndpoint,
                groundedPrompt.Length,
                language,
                _options.DefaultTemperature,
                timeoutSeconds);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.StreamingEndpoint.TrimStart('/'));

                /*
                 * Internal service-to-service authentication.
                 *
                 * Do NOT log this value.
                 */
                var headerAdded = request.Headers.TryAddWithoutValidation(InternalApiKeyHeader, _options.InternalApiKey);

                if (!headerAdded)
                {
                    throw new InvalidOperationException($"Unable to add " + $"{InternalApiKeyHeader} header.");
                }

                request.Content = JsonContent.Create(requestBody, options: JsonOptions);

                /*
                 * CRITICAL:
                 *
                 * ResponseHeadersRead prevents HttpClient from buffering
                 * the complete response before returning control.
                 *
                 * Without it, we lose the real streaming behaviour.
                 */
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, generationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(generationToken).ConfigureAwait(false);

                    _logger.LogError(
                        "[FASTAPI-AI][STREAM] " +
                        "HTTP failure. " +
                        "StatusCode={StatusCode}; " +
                        "ReasonPhrase={ReasonPhrase}; " +
                        "Body={Body}",
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        Truncate(errorBody, 500));

                    response.EnsureSuccessStatusCode();
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(generationToken).ConfigureAwait(false);

                using var reader = new StreamReader(responseStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false);

                while (true)
                {
                    generationToken.ThrowIfCancellationRequested();

                    var line = await reader.ReadLineAsync(generationToken).ConfigureAwait(false);

                    /*
                     * End of HTTP stream.
                     */
                    if (line is null)
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    FastApiGenerationChunkResponse? streamItem;

                    try
                    {
                        streamItem = JsonSerializer.Deserialize<FastApiGenerationChunkResponse>(line, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(
                            ex,
                            "[FASTAPI-AI][STREAM] " +
                            "Invalid NDJSON received. " +
                            "LinePreview={LinePreview}",
                            Truncate(line, 500));

                        throw new InvalidOperationException("OutZen.AI returned invalid NDJSON.", ex);
                    }

                    if (streamItem is null)
                    {
                        continue;
                    }

                    /*
                     * FastAPI can report an Ollama error inside
                     * the NDJSON stream after the HTTP 200 headers
                     * have already been sent.
                     */
                    if (!string.IsNullOrWhiteSpace(streamItem.Error))
                    {
                        throw new InvalidOperationException("OutZen.AI streaming error: " + streamItem.Error);
                    }

                    /*
                     * A real generated chunk.
                     */
                    if (!string.IsNullOrEmpty(streamItem.Chunk))
                    {
                        accumulatedResponse.Append(streamItem.Chunk);

                        chunkCount++;

                        _logger.LogDebug(
                            "[FASTAPI-AI][STREAM] " +
                            "Chunk received. " +
                            "ChunkNumber={ChunkNumber}; " +
                            "ChunkLength={ChunkLength}; " +
                            "TotalLength={TotalLength}",
                            chunkCount,
                            streamItem.Chunk.Length,
                            accumulatedResponse.Length);

                        /*
                         * THIS is the key point.
                         *
                         * Existing GptOrchestrator callback
                         * forwards this chunk to SignalR.
                         */
                        await onChunk(streamItem.Chunk).ConfigureAwait(false);
                    }

                    /*
                     * FastAPI/Ollama says generation is complete.
                     *
                     * Do NOT call onChunk("") here.
                     * GptOrchestrator already owns the SignalR
                     * IsFinal=true message.
                     */
                    if (streamItem.Done)
                    {
                        _logger.LogDebug(
                            "[FASTAPI-AI][STREAM] " +
                            "Done marker received. " +
                            "Provider={Provider}; " +
                            "Model={Model}",
                            streamItem.Provider,
                            streamItem.Model);

                        break;
                    }
                }

                var finalText = accumulatedResponse.ToString();

                if (string.IsNullOrWhiteSpace(finalText))
                {
                    throw new InvalidOperationException("OutZen.AI streaming endpoint " + "returned no generated content.");
                }

                _logger.LogInformation(
                    "[FASTAPI-AI][STREAM] Completed. " +
                    "Chunks={ChunkCount}; " +
                    "ResponseLength={ResponseLength}; " +
                    "ElapsedMs={ElapsedMs}",
                    chunkCount,
                    finalText.Length,
                    stopwatch.ElapsedMilliseconds);

                /*
                 * IMPORTANT:
                 *
                 * Return exactly the accumulated text.
                 * Do not reformat every chunk independently,
                 * otherwise spaces/newlines can be damaged.
                 */
                return finalText;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested && generationCts.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "[FASTAPI-AI][STREAM] " +
                    "Streaming timeout after " +
                    "{TimeoutSeconds}s. " +
                    "ChunksReceived={ChunkCount}; " +
                    "CharactersReceived={CharactersReceived}",
                    timeoutSeconds,
                    chunkCount,
                    accumulatedResponse.Length);

                throw new TimeoutException(
                    $"OutZen.AI streaming exceeded " +
                    $"{timeoutSeconds} seconds.",
                    ex);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "[FASTAPI-AI][STREAM] " +
                    "Generation cancelled by caller. " +
                    "ChunksReceived={ChunkCount}",
                    chunkCount);

                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "[FASTAPI-AI][STREAM] " +
                    "HTTP communication failure. " +
                    "BaseAddress={BaseAddress}; " +
                    "Endpoint={Endpoint}; " +
                    "ElapsedMs={ElapsedMs}",
                    _httpClient.BaseAddress?.ToString() ?? "<null>",
                    _options.StreamingEndpoint,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private Uri GetStreamingEndpoint()
        {
            var endpoint = _options.StreamingEndpoint.Trim();

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            return new Uri(endpoint.TrimStart('/'), UriKind.Relative);
        }


        // ============================================================
        // Legacy compatibility
        // ============================================================

        public Task<string> CallOllamaApi(string prompt, CancellationToken ct)
        {
            /*
             * Historical method name.
             *
             * ASP.NET does NOT call Ollama directly anymore.
             *
             * The call becomes:
             *
             * ASP.NET
             *   -> FastAPI
             *   -> Ollama
             *   -> Mistral
             */
            return GenerateFromPromptAsync(prompt, "fr-FR", ct);
        }


        public Task<int> ArchivePastGptInteractionsAsync()
        {
            /*
             * Archiving stays inside ASP.NET / SQL.
             *
             * There is no reason to send database maintenance
             * through FastAPI.
             */
            return _gptInteractionRepository.ArchivePastGptInteractionsAsync();
        }


        public Task<IEnumerable<Suggestion>> GetWeatherAdvisoryAsync(string location, CancellationToken ct = default)
        {
            /*
             * This method already existed on IMistralAIService,
             * but the former direct Ollama implementation did
             * not implement it either.
             *
             * Do NOT invent weather information here:
             * the FastAPI generation endpoint only receives a
             * grounded prompt and currently has no dedicated
             * weather-context contract.
             *
             * Weather recommendations should continue to be
             * built by the existing OutZen weather pipeline.
             */

            _logger.LogWarning(
                "[FASTAPI-AI] " +
                "GetWeatherAdvisoryAsync({Location}) " +
                "is a legacy IMistralAIService method. " +
                "No standalone grounded FastAPI weather " +
                "contract is configured; returning no " +
                "generated Suggestion.",
                location);

            return Task.FromResult<IEnumerable<Suggestion>>(Array.Empty<Suggestion>());
        }


        // ============================================================
        // Helpers
        // ============================================================

        private void EnsureConfiguration()
        {
            if (_httpClient.BaseAddress is null)
            {
                throw new InvalidOperationException("FastApiAI HttpClient BaseAddress " + "is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.GenerationEndpoint))
            {
                throw new InvalidOperationException("FastApiAI:GenerationEndpoint " + "is missing.");
            }

            if (string.IsNullOrWhiteSpace(_options.StreamingEndpoint))
            {
                throw new InvalidOperationException("FastApiAI:StreamingEndpoint " + "is missing.");
            }

            if (string.IsNullOrWhiteSpace(_options.InternalApiKey))
            {
                throw new InvalidOperationException("FastApiAI:InternalApiKey " + "is missing.");
            }

            if (_options.InternalApiKey.Length < 32)
            {
                throw new InvalidOperationException("FastApiAI:InternalApiKey " +   "is too short.");
            }
        }


        private Uri GetGenerationEndpoint()
        {
            var endpoint = _options.GenerationEndpoint.Trim();

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            return new Uri(endpoint.TrimStart('/'), UriKind.Relative);
        }


        private string BuildEffectiveGroundedPrompt(string groundedPrompt, string languageInstruction)
        {
            return $"""
                {groundedPrompt.Trim()} FINAL OUTPUT LANGUAGE INSTRUCTION: {languageInstruction}
                """;
        }

        private static string NormalizeLanguage(string? responseLanguage)
        {
            return string.IsNullOrWhiteSpace(responseLanguage) ? "fr-FR" : responseLanguage.Trim();
        }

        private void LogHttpFailure(HttpResponseMessage response, string? body, long elapsedMilliseconds)
        {
            _logger.LogWarning(
                "[FASTAPI-AI][GENERATE] " +
                "Non-success response. " +
                "StatusCode={StatusCode}; " +
                "ReasonPhrase={ReasonPhrase}; " +
                "BodyPreview={BodyPreview}; " +
                "ElapsedMs={ElapsedMs}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                Truncate(body, 500),
                elapsedMilliseconds);
        }


        private static string NormalizeGeneratedText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

            /*
             * Normalize horizontal whitespace but preserve
             * line breaks.
             */
            value = Regex.Replace(value, @"[ \t]{2,}", " ");
            value = Regex.Replace(value, @"[ \t]*\n[ \t]*", "\n");

            /*
             * Repair numbered items that Mistral sometimes
             * concatenates:
             *
             * "texte3. Parc"
             *
             * becomes
             *
             * "texte
             * 3. Parc"
             */
            value = Regex.Replace(value, @"(?<!^)(?<!\n)(?<!\d)" + @"([1-9]\.)\s*(?=\p{L})", Environment.NewLine + "$1 ");

            return value.Trim();
        }


        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            var normalized = value.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ").Trim();

            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return normalized[..maxLength] + "...";
        }
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.