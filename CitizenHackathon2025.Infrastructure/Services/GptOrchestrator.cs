using Azure.Core;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Extensions;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GptOrchestrator : IGptOrchestrator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<GPTHub, IGptClient> _hubContext;
        private readonly IGptRequestRegistry _gptRequestRegistry;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<GptOrchestrator> _logger;
        
        public GptOrchestrator(IServiceScopeFactory scopeFactory, IHubContext<GPTHub, IGptClient> hubContext, IGptRequestRegistry gptRequestRegistry, IHostApplicationLifetime appLifetime, ILogger<GptOrchestrator> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _gptRequestRegistry = gptRequestRegistry ?? throw new ArgumentNullException(nameof(gptRequestRegistry));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogWarning("[GPT GEO PATCH] Version 2026-07-18-v3 loaded: radius=25, maxCandidates=12");
        }

        public async Task<GptStartResponseDto> StartMistralRequestAsync(GptPromptRequest request, CancellationToken ct = default)
        {
            ValidateRequest(request);

            var prompt = request.Prompt.Trim();
            var interaction = await CreateInitialInteractionAsync(request, prompt, ct).ConfigureAwait(false);

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_appLifetime.ApplicationStopping);
            var requestId = _gptRequestRegistry.Register(interaction.Id, linkedCts);
            var startedAtUtc = DateTime.UtcNow;

            _ = Task.Run(
                () => RunPipelineAsync(interaction, request, requestId, linkedCts.Token),
                CancellationToken.None);

            _logger.LogInformation(
                "[GPT-PIPELINE][ASYNC] Accepted. InteractionId={InteractionId}, RequestId={RequestId}, PromptLength={PromptLength}",
                interaction.Id,
                requestId,
                prompt.Length);

            return new GptStartResponseDto
            {
                Accepted = true,
                InteractionId = interaction.Id,
                RequestId = requestId,
                StartedAtUtc = startedAtUtc,
                Status = "accepted",
                Message = "GPT request accepted and processing started."
            };
        }

        public async Task<GptInteractionDTO> RunMistralRequestAsync(GptPromptRequest request, CancellationToken ct = default)
        {
            ValidateRequest(request);

            var prompt = request.Prompt.Trim();

            _logger.LogInformation(
                "[GPT-PIPELINE][SYNC] Started. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, HttpTokenCanBeCanceled={HttpTokenCanBeCanceled}, HttpTokenIsCanceled={HttpTokenIsCanceled}",
                prompt.Length,
                request.Latitude,
                request.Longitude,
                ct.CanBeCanceled,
                ct.IsCancellationRequested);

            var interaction = await CreateInitialInteractionAsync(request, prompt, ct).ConfigureAwait(false);
            var interactionId = interaction.Id;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appLifetime.ApplicationStopping);
            var requestId = _gptRequestRegistry.Register(interactionId, linkedCts);

            try
            {
                await _hubContext.SendStarted(
                    new GptResponseStartedDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    });

                var finalDto = await ExecutePipelineInternalAsync(
                    request: request,
                    prompt: prompt,
                    interactionId: interactionId,
                    requestId: requestId,
                    ct: linkedCts.Token,
                    pushChunksToHub: false,
                    emitStartedEvent: false).ConfigureAwait(false);

                await _hubContext.SendCompleted(ToCompletedDto(finalDto));

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "completed",
                        Message = "Generation completed.",
                        TimestampUtc = DateTime.UtcNow
                    });

                return finalDto;
            }

            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE][SYNC] Cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);

                await MarkCancelledSafeAsync(interactionId,"Generation cancelled.", CancellationToken.None);

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "cancelled",
                        Message = "Generation cancelled.",
                        TimestampUtc = DateTime.UtcNow
                    });

                throw;
            }

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[GPT-PIPELINE][SYNC] Failed. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);

                await MarkFailedSafeAsync(interactionId, ex.Message).ConfigureAwait(false);

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "failed",
                        Message = ex.Message,
                        TimestampUtc = DateTime.UtcNow
                    });

                throw;
            }
            finally
            {
                _gptRequestRegistry.Remove(interactionId, requestId);

                _logger.LogInformation(
                    "[GPT-PIPELINE][SYNC] Cleanup done. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);
            }
        }

        public Task<bool> CancelAsync(int interactionId, string? requestId = null)
        {
            var cancelled = _gptRequestRegistry.TryCancel(interactionId, requestId);

            _logger.LogInformation(
                "[GPT-PIPELINE] Cancel requested. InteractionId={InteractionId}, RequestId={RequestId}, Cancelled={Cancelled}",
                interactionId,
                requestId,
                cancelled);

            return Task.FromResult(cancelled);
        }

        private async Task RunPipelineAsync(GPTInteraction interaction, GptPromptRequest request, string requestId, CancellationToken ct)
        {
            try
            {
                await _hubContext.SendStarted(
                    new GptResponseStartedDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    });

                var finalDto = await ExecutePipelineInternalAsync(
                    request: request,
                    prompt: request.Prompt.Trim(),
                    interactionId: interaction.Id,
                    requestId: requestId,
                    ct: ct,
                    pushChunksToHub: true,
                    emitStartedEvent: false)
                    .ConfigureAwait(false);

                await _hubContext.SendCompleted(
                    ToCompletedDto(finalDto));

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "completed",
                        Message = "Generation completed.",
                        TimestampUtc = DateTime.UtcNow
                    });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE][ASYNC] Cancelled. InteractionId={InteractionId}, RequestId={RequestId}, TokenCanBeCanceled={CanBeCanceled}, TokenIsCancellationRequested={IsCancellationRequested}",
                    interaction.Id,
                    requestId,
                    ct.CanBeCanceled,
                    ct.IsCancellationRequested);

                await MarkCancelledSafeAsync(
                    interaction.Id,
                    "Generation cancelled.",
                    CancellationToken.None);

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "cancelled",
                        Message = "Generation cancelled.",
                        TimestampUtc = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[GPT-PIPELINE][ASYNC] Failed. InteractionId={InteractionId}, RequestId={RequestId}",
                    interaction.Id,
                    requestId);

                await MarkFailedSafeAsync(
                        interaction.Id,
                        ex.Message)
                    .ConfigureAwait(false);

                await _hubContext.SendStatus(
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "failed",
                        Message = ex.Message,
                        TimestampUtc = DateTime.UtcNow
                    });
            }
            finally
            {
                _gptRequestRegistry.Remove(interaction.Id, requestId);
            }
        }
        private static void ValidateRequest(GptPromptRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Prompt))
                throw new ArgumentException("Prompt cannot be empty.", nameof(request));
        }

        private async Task<GPTInteraction> CreateInitialInteractionAsync(GptPromptRequest request, string prompt, CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var gptRepository = scope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();

            var interaction = new GPTInteraction
            {
                Prompt = prompt,
                Response = string.Empty,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                Model = "mistral",
                Temperature = 0.3f,
                SourceType = "MistralLocal",
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            var created = await gptRepository.CreatePendingAsync(interaction, ct).ConfigureAwait(false);

            if (created is null || created.Id <= 0)
                throw new InvalidOperationException("Unable to create GPT interaction.");

            _logger.LogInformation(
                "[GPT-PIPELINE] Initial interaction persisted. InteractionId={InteractionId}, PromptHash={PromptHash}, CreatedAt={CreatedAt}",
                created.Id,
                created.PromptHash,
                created.CreatedAt);

            return created;
        }

        private async Task<GptInteractionDTO> ExecutePipelineInternalAsync(GptPromptRequest request, string prompt, int interactionId, string requestId, CancellationToken ct, bool pushChunksToHub, bool emitStartedEvent)
        {
            var sw = Stopwatch.StartNew();

            if (emitStartedEvent)
            {
                await _hubContext.SendStarted(
                    new GptResponseStartedDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    });
            }

            await using var scope = _scopeFactory.CreateAsyncScope();

            var gptRepository = scope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();
            var localAiContextService = scope.ServiceProvider.GetRequiredService<ILocalAiContextService>();
            var mistralAiService = scope.ServiceProvider.GetRequiredService<IMistralAIService>();
            var placeRepository = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();

            var swContext = Stopwatch.StartNew();

            var contextPrompt = NormalizeMultilingualPromptForContext(prompt);

            _logger.LogInformation(
                "[GPT MULTILINGUAL] OriginalPrompt={OriginalPrompt}; " +
                "ContextPrompt={ContextPrompt}",
                prompt,
                contextPrompt);

            var placeNameResolver = scope.ServiceProvider.GetRequiredService<IPlaceNameResolver>();

            var requestedName = ExtractPlaceNameFromPrompt(contextPrompt);

            var places = await placeRepository.GetActivePlacesAsync(ct);

            var responseLanguage = ResolveResponseLanguage(prompt, request.LanguageCode);

            Place? originPlace = await placeNameResolver.ResolveAsync(prompt, responseLanguage, ct);

            if (originPlace is null)
            {
                originPlace = ResolvePlaceFromPrompt(contextPrompt, places);
            }

            if (originPlace is null && !string.IsNullOrWhiteSpace(requestedName))
            {
                originPlace = await placeRepository.FindByNameLikeAsync(requestedName, ct);
            }

            _logger.LogWarning(
                        "[GPT GEO DIAGNOSTIC] " +
                        "ContextPrompt={ContextPrompt}; " +
                        "RequestedName={RequestedName}; " +
                        "OriginPlace={OriginPlace}; " +
                        "OriginLat={OriginLat}; " +
                        "OriginLng={OriginLng}",
                        contextPrompt,
                        requestedName,
                        originPlace?.Name,
                        originPlace?.Latitude,
                        originPlace?.Longitude);

            double? effectiveLatitude = null;
            double? effectiveLongitude = null;

            if (originPlace is not null)
            {
                effectiveLatitude = (double)originPlace.Latitude;
                effectiveLongitude = (double)originPlace.Longitude;

                _logger.LogInformation(
                    "Origin resolved from SQL : {Name} ({Lat},{Lng})",
                    originPlace.Name,
                    effectiveLatitude,
                    effectiveLongitude);
            }
            else if (request.Latitude.HasValue &&
                     request.Longitude.HasValue)
            {
                effectiveLatitude = request.Latitude.Value;
                effectiveLongitude = request.Longitude.Value;

                _logger.LogInformation("Origin resolved from client GPS");

                _logger.LogInformation("Prompt reçu {Elapsed}", sw.Elapsed);
            }

            var localContext = await localAiContextService.BuildContextAsync(contextPrompt, effectiveLatitude, effectiveLongitude, ct).ConfigureAwait(false);

            swContext.Stop();

            _logger.LogInformation(
                "[GPT-PIPELINE] Local context built. InteractionId={InteractionId}, RequestId={RequestId}, ElapsedMs={ElapsedMs}, Places={Places}, Events={Events}, CrowdCalendar={CrowdCalendar}, CrowdInfo={CrowdInfo}, Traffic={Traffic}, Weather={Weather}, CriticalAlerts={CriticalAlerts}, HasChildren={HasChildren}, BadWeather={BadWeather}",
                interactionId,
                requestId,
                swContext.ElapsedMilliseconds,
                localContext.Places.Count,
                localContext.Events.Count,
                localContext.CrowdCalendar.Count,
                localContext.CrowdInfo.Count,
                localContext.Traffic.Count,
                localContext.Weather.Count,
                localContext.CriticalAlerts.Count,
                localContext.HasChildren,
                localContext.BadWeatherDetected);

            if (localContext.CriticalAlerts.Count > 0)
            {
                foreach (var alert in localContext.CriticalAlerts)
                {
                    _logger.LogWarning(
                        "[GPT-SAFETY] Confirmed critical alert in AI context. Kind={Kind}, Place={Place}, Severity={Severity}, DistanceKm={DistanceKm}, ExpiresAt={ExpiresAt}",
                        alert.AlertKind,
                        alert.PlaceName,
                        alert.Severity,
                        alert.DistanceKm,
                        alert.ExpiresAtUtc);
                }
            }

            string groundedPrompt = localAiContextService.BuildPrompt(localContext);

            groundedPrompt += $"""
                            ORIGINAL USER QUESTION — PRESERVE ITS LANGUAGE
                            {prompt}

                            Mandatory language rule:
                            - Answer in the language of the original user question.
                            - The normalized context question is used only for database search.
                            - Do not answer in the language of the normalized context question.
                            """;

            if (!string.IsNullOrWhiteSpace(requestedName) && originPlace is null)
            {
                groundedPrompt += $"""

                            Location requested by the user :
                            {requestedName}

                            Problem:
                            This location was not found in the SQL Place table.

                            Mandatory rules:
                            - Do not use the client's GPS coordinates to answer a question about this location.
                            - Do not invent any distances.
                            - Respond that the local OutZen data is insufficient to calculate alternatives around this location.
                            """;
            }

            _logger.LogInformation(
                "[GPT-PIPELINE] Grounded prompt built. InteractionId={InteractionId}, RequestId={RequestId}, GroundedPromptLength={GroundedPromptLength}, Preview={Preview}",
                interactionId,
                requestId,
                groundedPrompt.Length,
                groundedPrompt[..Math.Min(300, groundedPrompt.Length)]);

            _logger.LogInformation(
                "[GPT LANGUAGE] Requested={RequestedLanguage}; " +
                "Resolved={ResolvedLanguage}",
                request.LanguageCode,
                responseLanguage);

            if (effectiveLatitude.HasValue && effectiveLongitude.HasValue)
            {
                var geographicallyUniquePlaces = DeduplicatePlacesByCoordinates(places, duplicateRadiusKm: 0.1);

                var candidates = geographicallyUniquePlaces
                    .Where(p =>
                    {
                        if (originPlace is null)
                            return true;

                        var distanceFromOriginKm = GeoDistanceKm(
                            (double)originPlace.Latitude,
                            (double)originPlace.Longitude,
                            (double)p.Latitude,
                            (double)p.Longitude);

                        return distanceFromOriginKm > 0.1;
                    })

                    // Additional protection against strictly identical names.
                    .GroupBy(
                        p => p.Name.Trim(),
                        StringComparer.OrdinalIgnoreCase)

                    .Select(g => g.First())

                    .Select(p => new
                    {
                        p.Id,
                        Name = p.Name.Trim(),
                        p.Type,
                        p.Tag,
                        p.Indoor,
                        p.Capacity,

                        DistanceKm = GeoDistanceKm(
                            effectiveLatitude!.Value,
                            effectiveLongitude!.Value,
                            (double)p.Latitude,
                            (double)p.Longitude),

                        InterestScore = GetTouristicInterestScore(p)
                    })

                    // "Dans les environs" must allow approximately 25 km.
                    .Where(x => x.DistanceKm <= 25d)

                    .ToList();

                var nearest = candidates

                    // Tangible attractions take precedence over mere locations.
                    .OrderByDescending(x => x.InterestScore)
                    .ThenBy(x => x.DistanceKm)
                    .ThenBy(x => x.Name)
                    .Take(5)

                    // Final presentation by distance in the prompt.
                    .OrderBy(x => x.DistanceKm)
                    .ThenByDescending(x => x.InterestScore)
                    .ThenBy(x => x.Name)

                    .ToList();

                _logger.LogInformation(
                    "[NEARBY PIPELINE] Origin={Origin}; Latitude={Latitude}; Longitude={Longitude}; " +
                    "RadiusKm={RadiusKm}; Loaded={LoadedCount}; AfterGeoDedup={AfterGeoDedupCount}; " +
                    "InsideRadius={InsideRadiusCount}; SentToMistral={SentCount}",
                    originPlace?.Name ?? requestedName ?? "<unknown>",
                    effectiveLatitude.Value,
                    effectiveLongitude.Value,
                    25d,
                    places.Count,
                    geographicallyUniquePlaces.Count,
                    candidates.Count,
                    nearest.Count);

                foreach (var candidate in nearest)
                {
                    _logger.LogInformation(
                        "[NEARBY CANDIDATE] Id={Id}; Name={Name}; Type={Type}; " +
                        "DistanceKm={DistanceKm}; InterestScore={InterestScore}",
                        candidate.Id,
                        candidate.Name,
                        candidate.Type,
                        candidate.DistanceKm,
                        candidate.InterestScore);
                }

                var geoContext = JsonSerializer.Serialize(
                    nearest.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Type,
                        x.Indoor,
                        x.Capacity,
                        x.Tag,
                        x.DistanceKm,
                        x.InterestScore
                    }),
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                groundedPrompt += $"""

                                GEOGRAPHIC CONTEXT OUTZEN — SOURCE SQL dbo.Place

                                Location requested by the user :
                                {originPlace?.Name ?? requestedName ?? "Location not identified"}

                                Origin used to calculate distances :
                                Latitude={effectiveLatitude.Value}
                                Longitude={effectiveLongitude.Value}

                                Nearby places calculated by OutZen :
                                {geoContext}

                                Mandatory rules :
                                - Use only the places provided in "Nearby places calculated by OutZen".
                                - Use only the distances provided in DistanceKm.
                                - Never recalculate or invent a distance.
                                - Never invent a place, attraction or event.
                                - If no relevant place is provided, state that the available local data is insufficient.
                                - Prioritize actual tourist attractions over generic cities or villages.
                                - A city or village is geographical context, not an attraction by itself.
                                - Do not repeat generic safety phrases.
                                - Do not say "safer fallback zone" unless a confirmed critical alert exists.
                                - Do not detail capacities unless the user requests it.
                                - When at least three actual attractions are available, mention at least three.
                                - Group attractions by their nearby locality when this is useful.
                                - Mention a maximum of five actual attractions.
                                - Do not use all response slots for generic cities or villages.
                                - Do not detail capacities unless the user requests them.
                                - Answer concisely, but provide enough information to identify the attractions.
                                If no actual attraction or event is available:
                                - Do not describe a city or village as an interesting activity.
                                - State clearly that only nearby localities were found.
                                - Do not claim that they offer diversified activities unless such activities are present in the context.
                                """;

                groundedPrompt += """

                                GREETING RULE:
                                - If the user says "Bonsoir", begin the answer with "Bonsoir".
                                - If the user says "Bonjour", begin the answer with "Bonjour".
                                - Do not replace "Bonsoir" with "Bonjour".
                                - If the user uses no greeting, do not invent one.
                                """;

                _logger.LogInformation(
                    "[GPT GEO] RequestedName={RequestedName}, OriginPlace={OriginPlace}, OriginLat={OriginLat}, OriginLng={OriginLng}, NearestCount={NearestCount}",
                    requestedName,
                    originPlace?.Name,
                    effectiveLatitude.Value,
                    effectiveLongitude.Value,
                    nearest.Count);
            }
            else
            {
                groundedPrompt += """

                                GEOGRAPHICAL CONTEXT OUTZEN :
                                No reliable SQL location could be identified in the request.
                                Do not invent distances.
                                If you mention a distance, write "unknown distance".
                                Response constraints :
                                - Answer in a maximum of 6 lines.
                                - Provide details for a maximum of 5 locations only.
                                - Do not repeat "safer fallback zone" for each location.
                                - Only mention security if there is a real alert in the context.
                                - Never write "safer fallback zone" unless a confirmed critical alert is present in CriticalAlerts.
                                """;

                groundedPrompt += """

                                GREETING RULE:
                                - If the user says "Bonsoir", begin the answer with "Bonsoir".
                                - If the user says "Bonjour", begin the answer with "Bonjour".
                                - Do not replace "Bonsoir" with "Bonjour".
                                - If the user uses no greeting, do not invent one.
                                """;

                _logger.LogWarning(
                    "[GPT GEO] No reliable origin found. RequestedName={RequestedName}, RequestLat={RequestLat}, RequestLng={RequestLng}",
                    requestedName,
                    request.Latitude,
                    request.Longitude);
            }

            if (localContext.CriticalAlerts.Count == 0)
            {
                groundedPrompt += """
                                IMPORTANT SAFETY OVERRIDE:
                                - There is no confirmed safety alert in the supplied context.
                                - Do not use the words "safe", "safer", "safety", "secure",
                                    "plus sûr", "plus sûre", "sécurité", "zone de repli",
                                    or any equivalent expression.
                                - Present nearby places only as tourist alternatives,
                                    not as safety alternatives.
                                """;
            }

            if (localContext.CriticalAlerts.Count > 0)
            {
                groundedPrompt += """
                                CONFIRMED SAFETY ALERT:
                                - A confirmed alert is present in the context.
                                - You may explain the safety concern using only the supplied facts.
                                - Recommend alternatives outside the affected area.
                                """;
            }

            groundedPrompt += """
                            FINAL TOURISM SELECTION RULES:
                            - These rules override any earlier generic limit of 1 to 3 alternatives.
                            - For a general tourism request, mention 3 to 5 actual attractions when available.
                            - Do not select only the three nearest database records.
                            - Prioritize tourist attractions over cities and villages.
                            - Include relevant attractions up to 25 km from the resolved origin.
                            - Do not discuss children unless the user explicitly mentioned children or family.
                            - Do not add safety recommendations unless a confirmed alert exists.
                            """;

            var approximatePromptTokens = (int)Math.Ceiling(groundedPrompt.Length / 4d);

            _logger.LogWarning("[GPT FINAL PROMPT SIZE] " + "InteractionId={InteractionId}; " + "Characters={Characters}; " + "ApproximateTokens={ApproximateTokens}", interactionId, groundedPrompt.Length, approximatePromptTokens);

            string finalResponse;

            try
            {
                if (pushChunksToHub)
                {
                    groundedPrompt += """
                            FINAL RESPONSE LIMITS:
                            - Mention at most 5 places in the entire answer.
                            - Five places means five places total.
                            - Produce one list only.
                            - Prefer actual attractions over generic towns or villages.
                            - Every numbered item must be complete.
                            - Never stop after an unfinished word or sentence.
                            """;

                    finalResponse = await mistralAiService.StreamFromPromptAsync(
                        groundedPrompt, async chunkText =>
                        {
                            if (string.IsNullOrEmpty(chunkText))
                                return;

                            await _hubContext.SendChunk(
                                new GptResponseChunkDto
                                {
                                    InteractionId = interactionId,

                                    RequestId = requestId,

                                    Chunk = chunkText,

                                    IsFinal = false
                                });
                        },
                        responseLanguage:responseLanguage, ct: ct).ConfigureAwait(false);
                }
                else
                {
                    finalResponse = await mistralAiService.GenerateFromPromptAsync(
                        groundedPrompt: groundedPrompt,

                        responseLanguage: responseLanguage,

                        ct: ct
                    ).ConfigureAwait(false);
                }
            }
            catch (TimeoutException ex)
            {
                var verifiedCandidates = GetVerifiedTourismCandidates(localContext);

                finalResponse = BuildVerifiedTourismResponse(verifiedCandidates, responseLanguage);

                _logger.LogWarning(
                    ex,
                    "[GPT OLLAMA FALLBACK] " +
                    "Ollama timeout. " +
                    "InteractionId={InteractionId}; " +
                    "ResponseLanguage={ResponseLanguage}; " +
                    "VerifiedCandidates={VerifiedCandidates}; " +
                    "FallbackLength={FallbackLength}",
                    interactionId,
                    responseLanguage,
                    verifiedCandidates.Count,
                    finalResponse.Length);

                if (pushChunksToHub && !string.IsNullOrWhiteSpace(finalResponse))
                {
                    await _hubContext.SendChunk(new GptResponseChunkDto
                    {
                        InteractionId = interactionId,

                        RequestId = requestId,

                        Chunk = finalResponse,

                        IsFinal = false
                    });
                }
            }

            _logger.LogInformation(
                "[GPT-PIPELINE] Generation finished. " +
                "InteractionId={InteractionId}; " +
                "RequestId={RequestId}; " +
                "ResponseLength={ResponseLength}",
                interactionId,
                requestId,
                finalResponse?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(finalResponse))
            {
                finalResponse = responseLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
                    ? "OutZen не получил ответа от локальной модели."

                    : responseLanguage.StartsWith("fr", StringComparison.OrdinalIgnoreCase)

                    ? "Aucune réponse n’a été générée par le modèle local."

                    : "No response was generated by the local model.";
            }

            _logger.LogInformation("[GPT-PIPELINE] Mistral generation finished. InteractionId={InteractionId}, RequestId={RequestId}, ResponseLength={ResponseLength}", interactionId, requestId, finalResponse?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(finalResponse))
            {
                finalResponse = responseLanguage.StartsWith("fr", StringComparison.OrdinalIgnoreCase)
                        ? "Aucune réponse n’a été générée par Mistral." : "No response from Mistral.";
            }

            finalResponse = SanitizeUnsupportedSafetyClaims(finalResponse, hasConfirmedCriticalAlert: localContext.CriticalAlerts.Count > 0);

            finalResponse = FilterUnsupportedTourismItems(finalResponse, localContext, responseLanguage);

            var updated = await gptRepository.UpdateResponseAsync(interactionId, finalResponse, ct).ConfigureAwait(false);

            if (!updated)
            {
                throw new InvalidOperationException($"Failed to persist final GPT response for interaction {interactionId}.");
            }

            var suggestionRepository = scope.ServiceProvider.GetService<ISuggestionRepository>();

            if (suggestionRepository is not null && localContext.CriticalAlerts.Count > 0)
            {
                // Creation of the security suggestion.
            }

            var persisted = await gptRepository
                .GetByIdAsync(interactionId)
                .ConfigureAwait(false);

            if (persisted is null)
            {
                throw new InvalidOperationException($"GPT interaction {interactionId} not found after update.");
            }

            var finalDto = persisted.MapToGptInteractionDTO();

            if (pushChunksToHub)
            {
                await _hubContext.SendChunk(
                    new GptResponseChunkDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Chunk = string.Empty,
                        IsFinal = true
                    });
            }

            _logger.LogInformation(
                "[GPT-PIPELINE] Final interaction persisted. InteractionId={InteractionId}, RequestId={RequestId}, TotalElapsedMs={ElapsedMs}, PersistedResponseLength={PersistedResponseLength}",
                finalDto.Id,
                requestId,
                sw.ElapsedMilliseconds,
                finalDto.Response?.Length ?? 0);

            return finalDto;

        }

        private static string SanitizeUnsupportedSafetyClaims(string response, bool hasConfirmedCriticalAlert)
        {
            if (hasConfirmedCriticalAlert ||
                string.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            var replacements = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                // French
                ["une destination plus sûre"] = "une destination proche",
                ["des destinations plus sûres"] = "des destinations proches",
                ["une option plus sûre"] = "une autre option",
                ["des options plus sûres"] = "d’autres options",
                ["un endroit plus sûr"] = "un endroit proche",
                ["des endroits plus sûrs"] = "des endroits proches",
                ["une zone plus sûre"] = "une zone proche",
                ["une zone de repli plus sûre"] = "une localité proche",
                ["pour votre sécurité"] = "pour votre visite",
                ["en toute sécurité"] = "dans de bonnes conditions",

                // English
                ["a safer destination"] = "a nearby destination",
                ["safer destinations"] = "nearby destinations",
                ["a safer option"] = "another option",
                ["safer options"] = "other options",
                ["a safer place"] = "a nearby place",
                ["safer places"] = "nearby places",
                ["a safer fallback area"] = "a nearby location",
                ["for your safety"] = "for your visit"
            };

            var result = response;

            foreach (var replacement in replacements)
            {
                result = Regex.Replace(
                    result,
                    Regex.Escape(replacement.Key),
                    replacement.Value,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant);
            }

            return result.Trim();
        }

        private static bool AreLikelyGeographicDuplicates(Place first, Place second, double duplicateRadiusKm)
        {
            var distanceKm = GeoDistanceKm(
                (double)first.Latitude,
                (double)first.Longitude,
                (double)second.Latitude,
                (double)second.Longitude);

            if (distanceKm > duplicateRadiusKm)
                return false;

            var firstType = first.Type?.Trim();
            var secondType = second.Type?.Trim();

            var normalizedFirstName = NormalizePlaceIdentity(first.Name);
            var normalizedSecondName = NormalizePlaceIdentity(second.Name);

            var similarName =
                normalizedFirstName == normalizedSecondName ||
                normalizedFirstName.Contains(
                    normalizedSecondName,
                    StringComparison.OrdinalIgnoreCase) ||
                normalizedSecondName.Contains(
                    normalizedFirstName,
                    StringComparison.OrdinalIgnoreCase);

            var bothLookLikeLocalities = IsLocalityType(firstType) && IsLocalityType(secondType);
            var veryCloseLocalities = bothLookLikeLocalities && distanceKm <= 0.03d;

            return similarName || veryCloseLocalities;
        }

        private static string NormalizePlaceIdentity(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var normalized = name
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            normalized = new string(
                normalized
                    .Where(c =>
                        CharUnicodeInfo.GetUnicodeCategory(c) !=
                        UnicodeCategory.NonSpacingMark)
                    .ToArray());

            normalized = normalized
                .Replace("-", " ")
                .Replace("’", "'");

            normalized = Regex.Replace(
                normalized,
                @"\bst\b",
                "saint",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

            normalized = Regex.Replace(
                normalized,
                @"\s+",
                " ");

            return normalized.Trim();
        }

        private static bool IsLocalityType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            return type.Contains("ville", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("village", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("commune", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("localité", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("localite", StringComparison.OrdinalIgnoreCase);
        }
        private static bool TryExtractCoordinatesFromPrompt(string? prompt, out double latitude, out double longitude)
        {
            latitude = default;
            longitude = default;

            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            // Match:
            // 50.434780,5.876832
            // (50.434780,5.876832)
            // 50,434780 ; 5,876832
            var match = Regex.Match(prompt, @"(?<lat>[+-]?\d{1,2}\.\d+)\s*[,;]\s*(?<lng>[+-]?\d{1,3}\.\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            if (!match.Success)
                return false;

            var latText = match.Groups["lat"].Value.Replace(',', '.');
            var lngText = match.Groups["lng"].Value.Replace(',', '.');

            if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude))
            {
                return false;
            }

            if (!double.TryParse(lngText, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
            {
                return false;
            }

            return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
        }

        private string FilterUnsupportedTourismItems(string response, LocalAiContextDTO context, string responseLanguage)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            var verifiedCandidates = GetVerifiedTourismCandidates(context);

            var allowedNames =
                verifiedCandidates
                    .Select(candidate =>
                        NormalizeFactKey(
                            candidate.Name))
                    .Where(name =>
                        !string.IsNullOrWhiteSpace(name))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (allowedNames.Count == 0)
            {
                return BuildNoLocalResultMessage(responseLanguage);
            }

            var normalizedResponse = response
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            var lines = normalizedResponse
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = new List<string>();

            var acceptedItems = 0;

            foreach (var line in lines)
            {
                var numberedItem = Regex.Match(line, @"^\s*(?:\d{1,2}[.)]|[-•])\s*(.+)$", RegexOptions.CultureInvariant);

                if (!numberedItem.Success)
                {
                    result.Add(line);
                    continue;
                }

                var content = numberedItem.Groups[1].Value.Trim();

                var normalizedContent = NormalizeFactKey(content);

                var supported = allowedNames.Any(allowedName =>
                    normalizedContent.Contains(allowedName, StringComparison.OrdinalIgnoreCase));

                if (!supported)
                    continue;

                acceptedItems++;

                result.Add($"{acceptedItems}. {content}");
            }

            _logger.LogWarning(
                "[GPT FACT FILTER] " +
                "VerifiedCandidates={VerifiedCandidates}; " +
                "AllowedNames={AllowedNames}; " +
                "AcceptedItems={AcceptedItems}; " +
                "OriginalResponseLength={OriginalResponseLength}",
                verifiedCandidates.Count,
                allowedNames.Count,
                acceptedItems,
                response.Length);

            if (acceptedItems == 0)
            {
                return BuildVerifiedTourismResponse(verifiedCandidates, responseLanguage);
            }

            return string.Join(Environment.NewLine, result);
        }

        private static string NormalizeFactKey(string value)
        {
            var decomposed = value.Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(decomposed.Length);

            foreach (var character in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return Regex.Replace(builder.ToString(), @"[^\p{L}\p{N}]+", " ").Trim();
        }

        private static string BuildNoLocalResultMessage(string responseLanguage)
        {
            if (responseLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    В локальных данных OutZen недостаточно
                    проверенной информации, чтобы предложить
                    достопримечательности для этого места.
                    """;
            }

            if (responseLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    OutZen does not have enough verified local data
                    to recommend attractions for this location.
                    """;
            }

            return """
                Les données locales vérifiées d’OutZen
                sont insuffisantes pour proposer des attractions
                autour de cette localité.
                """;
        }
        private async Task MarkFailedSafeAsync(int interactionId, string? errorMessage)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var gptRepository = scope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();

                await gptRepository.MarkFailedAsync(interactionId, errorMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE] Failed to mark interaction as failed. InteractionId={InteractionId}",
                    interactionId);
            }
        }
 
        private static GptInteractionCompletedDto ToCompletedDto(GptInteractionDTO dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            return new GptInteractionCompletedDto
            {
                Id = dto.Id,
                Prompt = dto.Prompt ?? string.Empty,
                Response = dto.Response ?? string.Empty,
                PromptHash = dto.PromptHash,
                CreatedAt = dto.CreatedAt,
                Active = dto.Active,

                EventId = dto.EventId,
                CrowdInfoId = dto.CrowdInfoId,
                PlaceId = dto.PlaceId,
                TrafficConditionId = dto.TrafficConditionId,
                WeatherForecastId = dto.WeatherForecastId,

                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                SourceType = dto.SourceType,
                CrowdLevel = dto.CrowdLevel
            };
        }

        private static string? ExtractPlaceNameFromPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            var markers = new[]
            {
                "du côté de",
                "du coté de",
                "autour de",
                "près de",
                "pres de",
                "proche de",
                "aux alentours de",
                "dans les environs de",
                "à ",
                "a "
            };

            foreach (var marker in markers)
            {
                var index = prompt.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    continue;

                var value = prompt[(index + marker.Length)..]
                    .Trim(' ', '?', '!', '.', ',', ';', ':', '\r', '\n', '\t');

                value = Regex.Replace(
                        value,
                        @"\b(cette semaine|ce week-end|ce weekend|aujourd'hui|demain|maintenant|en ce moment|pour ce week-end|pour ce weekend)\b.*$",
                        "",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    .Trim(' ', '?', '!', '.', ',', ';', ':', '\r', '\n', '\t');

                value = Regex.Replace(value, @"\s+", " ").Trim();

                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return null;
        }

        private static Place? ResolvePlaceFromPrompt(string prompt, IReadOnlyList<Place> places)
        {
            if (string.IsNullOrWhiteSpace(prompt) || places.Count == 0)
                return null;

            var normalizedPrompt = NormalizeSearchText(prompt);

            return places
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .OrderByDescending(p => p.Name.Length)
                .FirstOrDefault(p =>
                    normalizedPrompt.Contains(
                        NormalizeSearchText(p.Name),
                        StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeSearchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("’", "'")
                .Replace("?", " ")
                .Replace("!", " ")
                .Replace(",", " ")
                .Replace(".", " ")
                .Trim();
        }
        private static double GeoDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0088;

            static double ToRad(double deg) => deg * Math.PI / 180.0;

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) *
                Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return Math.Round(earthRadiusKm * c, 2);
        }

        private static IReadOnlyList<Place> DeduplicatePlacesByCoordinates(IEnumerable<Place> places, double duplicateRadiusKm = 0.1)
        {
            ArgumentNullException.ThrowIfNull(places);

            var result = new List<Place>();

            foreach (var place in places
                         .Where(p =>
                             p is not null &&
                             !string.IsNullOrWhiteSpace(p.Name))
                         .OrderBy(p => p.Id))
            {
                var latitude = (double)place.Latitude;
                var longitude = (double)place.Longitude;

                var existingIndex = result.FindIndex(existing =>
                    AreLikelyGeographicDuplicates(
                        place,
                        existing,
                        duplicateRadiusKm));

                if (existingIndex < 0)
                {
                    result.Add(place);
                    continue;
                }

                var existingPlace = result[existingIndex];

                if (IsBetterCanonicalPlace(place, existingPlace))
                {
                    result[existingIndex] = place;
                }
            }

            return result;
        }

        private static bool IsBetterCanonicalPlace(Place candidate, Place current)
        {
            var candidateScore = GetPlaceMetadataScore(candidate);
            var currentScore = GetPlaceMetadataScore(current);

            if (candidateScore != currentScore)
                return candidateScore > currentScore;

            var candidateName = candidate.Name?.Trim() ?? string.Empty;
            var currentName = current.Name?.Trim() ?? string.Empty;

            var candidateAbbreviated = IsAbbreviatedSaintName(candidateName);
            var currentAbbreviated = IsAbbreviatedSaintName(currentName);

            if (candidateAbbreviated != currentAbbreviated)
                return !candidateAbbreviated;

            var candidateHasHyphen = candidateName.Contains('-');
            var currentHasHyphen = currentName.Contains('-');

            if (candidateHasHyphen != currentHasHyphen)
                return candidateHasHyphen;

            return candidate.Id < current.Id;
        }

        private static bool IsAbbreviatedSaintName(string name)
        {
            return name.StartsWith(
                       "St ",
                       StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith(
                       "St-",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static int GetPlaceMetadataScore(Place place)
        {
            var score = 0;

            if (!string.IsNullOrWhiteSpace(place.Type))
                score++;

            if (!string.IsNullOrWhiteSpace(place.Tag))
                score++;

            if (place.Capacity > 0)
                score++;

            return score;
        }

        private static int GetTouristicInterestScore(Place place)
        {
            ArgumentNullException.ThrowIfNull(place);

            var searchableText = string.Join(
                ' ',
                place.Name ?? string.Empty,
                place.Type ?? string.Empty,
                place.Tag ?? string.Empty);

            if (searchableText.Contains(
                    "tourist",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            if (searchableText.Contains(
                    "mémorial",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "memorial",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "monument",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "château",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "chateau",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 90;
            }

            if (searchableText.Contains(
                    "parc",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "museum",
                    StringComparison.OrdinalIgnoreCase) ||
                searchableText.Contains(
                    "musée",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 80;
            }

            if (IsLocalityType(place.Type))
                return 20;

            return 50;
        }

        private static string ResolveResponseLanguage(string prompt, string? requestedLanguage)
        {
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                // Cyrillic alphabet.
                if (Regex.IsMatch(prompt, @"[\u0400-\u04FF]"))
                {
                    return "ru-RU";
                }

                // Chinese characters.
                if (Regex.IsMatch(prompt, @"[\u4E00-\u9FFF]"))
                {
                    return "zh-CN";
                }

                // Arabic alphabet.
                if (Regex.IsMatch(prompt, @"[\u0600-\u06FF]"))
                {
                    return "ar";
                }
            }

            return string.IsNullOrWhiteSpace(requestedLanguage) ? "fr-FR" : requestedLanguage.Trim();
        }

        private static string NormalizeMultilingualPromptForContext(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return string.Empty;

            var result = prompt.Trim();

            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            // Geographical expressions.
            result = Regex.Replace(result, @"\bв\s+окрестностях\b", "autour de", options);
            result = Regex.Replace(result, @"\bрядом\s+с\b", "près de", options);
            // Temporal expressions.
            result = Regex.Replace(result, @"\bсегодня\b", "aujourd'hui", options);
            result = Regex.Replace(result, @"\bзавтра\b", "demain",options);
            result = Regex.Replace(result, @"\b(в\s+эти\s+выходные|на\s+выходных)\b", "ce week-end", options);
            result = Regex.Replace(result, @"\b(на\s+этой\s+неделе|на\s+неделе)\b", "cette semaine", options);
            // Tourist intentions.
            result = Regex.Replace(result, @"что\s+интересного\s+(есть|можно\s+увидеть)?", "quoi faire", options);
            result = Regex.Replace(result, @"что\s+посмотреть", "quoi faire", options);
            result = Regex.Replace(result, @"куда\s+сходить", "quoi faire", options);
            
            return Regex.Replace(result, @"\s+", " ").Trim();

        }
        private async Task MarkCancelledSafeAsync(int interactionId, string? message, CancellationToken ct = default)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var repository =
                    scope.ServiceProvider
                        .GetRequiredService<IGptInteractionRepository>();

                await repository.MarkCancelledAsync(
                        interactionId,
                        message,
                        ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "[GPT-PIPELINE] MarkCancelledSafeAsync cancelled. InteractionId={InteractionId}",
                    interactionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE] Failed to mark interaction as cancelled. InteractionId={InteractionId}",
                    interactionId);
            }
        }
        private sealed record VerifiedTourismCandidate(string Name, double? DistanceKm, string? Type);

        private static IReadOnlyList<VerifiedTourismCandidate>GetVerifiedTourismCandidates(LocalAiContextDTO context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var placeCandidates = context.Places
                .Where(place => !string.IsNullOrWhiteSpace(place.Name))
                .Select(place => new VerifiedTourismCandidate(place.Name!.Trim(), place.DistanceKm, place.Type));

            var keywordCandidates = context.KeywordMatchedPlaces
                .Where(place => !string.IsNullOrWhiteSpace(place.Name))
                .Select(place => new VerifiedTourismCandidate(place.Name!.Trim(), place.DistanceKm, place.Type));

            var eventCandidates = context.Events
                .Where(currentEvent => !string.IsNullOrWhiteSpace(currentEvent.Title))
                .Select(currentEvent => new VerifiedTourismCandidate(currentEvent.Title!.Trim(), currentEvent.DistanceKm, "Event"));

            return placeCandidates
                .Concat(keywordCandidates)
                .Concat(eventCandidates)
                .GroupBy(candidate => NormalizeFactKey(candidate.Name), StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderBy(candidate => candidate.DistanceKm ?? double.MaxValue)
                    .First())
                .OrderBy(candidate => candidate.DistanceKm ?? double.MaxValue)
                .ThenBy(candidate => candidate.Name)
                .ToList();
        }

        private static string BuildVerifiedTourismResponse(IReadOnlyList<VerifiedTourismCandidate> candidates, string responseLanguage)
        {
            if (candidates.Count == 0)
            {
                return BuildNoLocalResultMessage(responseLanguage);
            }

            var selected = candidates
                .Take(5)
                .ToList();

            var result = new StringBuilder();

            if (responseLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("Вот несколько мест, проверенных " + "по локальным данным OutZen:");
            }
            else if (responseLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("Here are several places verified " + "in OutZen's local data:");
            }
            else
            {
                result.AppendLine("Voici plusieurs lieux vérifiés " + "dans les données locales d’OutZen :");
            }

            result.AppendLine();

            for (var index = 0; index < selected.Count; index++)
            {
                var candidate = selected[index];

                var distance = candidate.DistanceKm.HasValue ? candidate.DistanceKm.Value.ToString(
                          "0.##", CultureInfo.InvariantCulture) + " km" : responseLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "расстояние недоступно" : "distance non disponible";

                result.Append($"{index + 1}. " + $"{candidate.Name} — " + $"{distance}");

                if (!string.IsNullOrWhiteSpace(candidate.Type))
                {
                    result.Append($" — {candidate.Type}");
                }

                result.AppendLine(".");
            }

            if (responseLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(Environment.NewLine + "Приятного открытия!");
            }
            else
            {
                result.Append(Environment.NewLine + "Bonne découverte.");
            }

            return result.ToString().Trim();
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.