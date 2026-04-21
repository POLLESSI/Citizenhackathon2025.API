using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GptOrchestrator : IGptOrchestrator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<GPTHub> _hubContext;
        private readonly IGptRequestRegistry _gptRequestRegistry;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<GptOrchestrator> _logger;

        public GptOrchestrator(
            IServiceScopeFactory scopeFactory,
            IHubContext<GPTHub> hubContext,
            IGptRequestRegistry gptRequestRegistry,
            IHostApplicationLifetime appLifetime,
            ILogger<GptOrchestrator> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _gptRequestRegistry = gptRequestRegistry ?? throw new ArgumentNullException(nameof(gptRequestRegistry));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GptStartResponseDto> StartMistralRequestAsync(
            GptPromptRequest request,
            CancellationToken ct = default)
        {
            ValidateRequest(request);

            var prompt = request.Prompt.Trim();
            var interaction = await CreateInitialInteractionAsync(request, prompt, ct).ConfigureAwait(false);

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appLifetime.ApplicationStopping);
            var requestId = _gptRequestRegistry.Register(interaction.Id, linkedCts);

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
                StartedAtUtc = DateTime.UtcNow,
                Status = "accepted",
                Message = "GPT request accepted and processing started."
            };
        }

        public async Task<GptInteractionDTO> RunMistralRequestAsync(
            GptPromptRequest request,
            CancellationToken ct = default)
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
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStarted",
                    new GptResponseStartedDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);

                var finalDto = await ExecutePipelineInternalAsync(
                    request: request,
                    prompt: prompt,
                    interactionId: interactionId,
                    requestId: requestId,
                    ct: linkedCts.Token,
                    pushChunksToHub: false,
                    emitStartedEvent: false).ConfigureAwait(false);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "completed",
                        Message = "Generation completed.",
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);

                return finalDto;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE][SYNC] Cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "cancelled",
                        Message = "Generation cancelled.",
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);

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

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Status = "failed",
                        Message = ex.Message,
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);

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

        private async Task RunPipelineAsync(
            GPTInteraction interaction,
            GptPromptRequest request,
            string requestId,
            CancellationToken ct)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStarted",
                    new GptResponseStartedDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);

                await ExecutePipelineInternalAsync(
                    request: request,
                    prompt: request.Prompt.Trim(),
                    interactionId: interaction.Id,
                    requestId: requestId,
                    ct: ct,
                    pushChunksToHub: true,
                    emitStartedEvent: false).ConfigureAwait(false);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "completed",
                        Message = "Generation completed.",
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[GPT-PIPELINE][ASYNC] Cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    interaction.Id,
                    requestId);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "cancelled",
                        Message = "Generation cancelled.",
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[GPT-PIPELINE][ASYNC] Failed. InteractionId={InteractionId}, RequestId={RequestId}",
                    interaction.Id,
                    requestId);

                await MarkFailedSafeAsync(interaction.Id, ex.Message).ConfigureAwait(false);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStatus",
                    new GptResponseStatusDto
                    {
                        InteractionId = interaction.Id,
                        RequestId = requestId,
                        Status = "failed",
                        Message = ex.Message,
                        TimestampUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);
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

        private async Task<GPTInteraction> CreateInitialInteractionAsync(
            GptPromptRequest request,
            string prompt,
            CancellationToken ct)
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

        private async Task<GptInteractionDTO> ExecutePipelineInternalAsync(
            GptPromptRequest request,
            string prompt,
            int interactionId,
            string requestId,
            CancellationToken ct,
            bool pushChunksToHub,
            bool emitStartedEvent)
        {
            var sw = Stopwatch.StartNew();

            if (emitStartedEvent)
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStarted",
                    new GptResponseStartedDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        StartedAtUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);
            }

            await using var scope = _scopeFactory.CreateAsyncScope();

            var gptRepository = scope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();
            var localAiContextService = scope.ServiceProvider.GetRequiredService<ILocalAiContextService>();
            var mistralAiService = scope.ServiceProvider.GetRequiredService<IMistralAIService>();

            var swContext = Stopwatch.StartNew();

            var localContext = await localAiContextService.BuildContextAsync(
                prompt,
                request.Latitude,
                request.Longitude,
                ct).ConfigureAwait(false);

            swContext.Stop();

            _logger.LogInformation(
                "[GPT-PIPELINE] Local context built. InteractionId={InteractionId}, RequestId={RequestId}, ElapsedMs={ElapsedMs}, Places={Places}, Events={Events}, CrowdCalendar={CrowdCalendar}, CrowdInfo={CrowdInfo}, Traffic={Traffic}, Weather={Weather}",
                interactionId,
                requestId,
                swContext.ElapsedMilliseconds,
                localContext.Places.Count,
                localContext.Events.Count,
                localContext.CrowdCalendar.Count,
                localContext.CrowdInfo.Count,
                localContext.Traffic.Count,
                localContext.Weather.Count);

            var groundedPrompt = localAiContextService.BuildPrompt(localContext);

            _logger.LogInformation(
                "[GPT-PIPELINE] Grounded prompt built. InteractionId={InteractionId}, RequestId={RequestId}, GroundedPromptLength={GroundedPromptLength}, Preview={Preview}",
                interactionId,
                requestId,
                groundedPrompt.Length,
                groundedPrompt[..Math.Min(300, groundedPrompt.Length)]);

            string finalResponse;

            if (pushChunksToHub)
            {
                var chunkCount = 0;
                var streamedChars = 0;

                finalResponse = await mistralAiService.StreamFromPromptAsync(
                    groundedPrompt,
                    async chunkText =>
                    {
                        chunkCount++;
                        streamedChars += chunkText?.Length ?? 0;

                        _logger.LogInformation(
                            "[GPT-PIPELINE] Chunk received. InteractionId={InteractionId}, RequestId={RequestId}, ChunkIndex={ChunkIndex}, ChunkLength={ChunkLength}, TotalChars={TotalChars}",
                            interactionId,
                            requestId,
                            chunkCount,
                            chunkText?.Length ?? 0,
                            streamedChars);

                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveGptResponseChunk",
                            new GptResponseChunkDto
                            {
                                InteractionId = interactionId,
                                RequestId = requestId,
                                Chunk = chunkText ?? string.Empty,
                                IsFinal = false
                            },
                            CancellationToken.None);
                    },
                    ct).ConfigureAwait(false);
            }
            else
            {
                finalResponse = await mistralAiService.GenerateFromPromptAsync(
                    groundedPrompt,
                    ct).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(finalResponse))
                finalResponse = "No response from Mistral.";

            var updated = await gptRepository.UpdateResponseAsync(
                interactionId,
                finalResponse,
                ct).ConfigureAwait(false);

            if (!updated)
                throw new InvalidOperationException($"Failed to persist final GPT response for interaction {interactionId}.");

            var persisted = await gptRepository.GetByIdAsync(interactionId).ConfigureAwait(false);

            if (persisted is null)
                throw new InvalidOperationException($"GPT interaction {interactionId} not found after update.");

            var finalDto = persisted.MapToGptInteractionDTO();

            if (pushChunksToHub)
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseChunk",
                    new GptResponseChunkDto
                    {
                        InteractionId = interactionId,
                        RequestId = requestId,
                        Chunk = string.Empty,
                        IsFinal = true
                    },
                    CancellationToken.None);
            }

            _logger.LogInformation(
                "[GPT-PIPELINE] Final interaction persisted. InteractionId={InteractionId}, RequestId={RequestId}, TotalElapsedMs={ElapsedMs}, PersistedResponseLength={PersistedResponseLength}",
                finalDto.Id,
                requestId,
                sw.ElapsedMilliseconds,
                finalDto.Response?.Length ?? 0);

            return finalDto;
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
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.