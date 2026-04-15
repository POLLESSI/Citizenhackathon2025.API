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
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _gptRequestRegistry = gptRequestRegistry;
            _appLifetime = appLifetime;
            _logger = logger;
        }

        public async Task<GptStartResponseDto> StartMistralRequestAsync(
            GptPromptRequest request,
            CancellationToken ct = default)
        {
            Console.WriteLine("### GPT ORCHESTRATOR START CALLED ###");

            ValidateRequest(request);

            var prompt = request.Prompt.Trim();
            var startedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "[GPT-PIPELINE][ASYNC] ASK-MISTRAL accepted. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, HttpTokenCanBeCanceled={HttpTokenCanBeCanceled}, HttpTokenIsCanceled={HttpTokenIsCanceled}",
                prompt.Length,
                request.Latitude,
                request.Longitude,
                ct.CanBeCanceled,
                ct.IsCancellationRequested);

            var created = await CreateInitialInteractionAsync(request, prompt, CancellationToken.None);
            var interactionId = created.Id;

            // Do not link to the HTTP token here under any circumstances:
            // otherwise the pipeline is cancelled as soon as the 202 is returned.
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_appLifetime.ApplicationStopping);
            var requestId = _gptRequestRegistry.Register(interactionId, linkedCts);

            _logger.LogInformation(
                "[GPT-PIPELINE][ASYNC] Request registered. InteractionId={InteractionId}, RequestId={RequestId}, BackgroundTokenCanBeCanceled={CanBeCanceled}, BackgroundTokenIsCanceled={IsCanceled}",
                interactionId,
                requestId,
                linkedCts.Token.CanBeCanceled,
                linkedCts.Token.IsCancellationRequested);

            await _hubContext.Clients.All.SendAsync(
                "ReceiveGptResponseStarted",
                interactionId,
                requestId,
                CancellationToken.None);

            _logger.LogInformation(
                "[GPT-PIPELINE][ASYNC] Start event sent to hub. InteractionId={InteractionId}, RequestId={RequestId}",
                interactionId,
                requestId);

            _ = Task.Run(async () =>
            {
                Console.WriteLine($"### BACKGROUND START interactionId={interactionId} ###");

                try
                {
                    var finalDto = await ExecutePipelineInternalAsync(
                        request: request,
                        prompt: prompt,
                        interactionId: interactionId,
                        requestId: requestId,
                        ct: linkedCts.Token,
                        pushChunksToHub: true,
                        emitStartedEvent: false);

                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveGptResponseChunk",
                        finalDto.Id,
                        requestId,
                        string.Empty,
                        true,
                        CancellationToken.None);

                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveGptResponse",
                        finalDto,
                        CancellationToken.None);

                    _logger.LogInformation(
                        "[GPT-PIPELINE][ASYNC] Final hub payload sent. InteractionId={InteractionId}, RequestId={RequestId}",
                        finalDto.Id,
                        requestId);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "[GPT-PIPELINE][ASYNC] Request cancelled. InteractionId={InteractionId}, RequestId={RequestId}, AppStopping={AppStopping}, TokenIsCanceled={TokenIsCanceled}",
                        interactionId,
                        requestId,
                        _appLifetime.ApplicationStopping.IsCancellationRequested,
                        linkedCts.Token.IsCancellationRequested);

                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveGptResponseCancelled",
                        interactionId,
                        requestId,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"### GPT PIPELINE EXCEPTION TYPE={ex.GetType().FullName} ###");
                    Console.WriteLine($"### GPT PIPELINE EXCEPTION MESSAGE={ex.Message} ###");
                    Console.WriteLine($"### GPT PIPELINE EXCEPTION STACK={ex} ###");

                    _logger.LogError(
                        ex,
                        "[GPT-PIPELINE][ASYNC] Request failed. InteractionId={InteractionId}, RequestId={RequestId}",
                        interactionId,
                        requestId);

                    try
                    {
                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveGptResponseFailed",
                            interactionId,
                            ex.Message,
                            CancellationToken.None);
                    }
                    catch (Exception hubEx)
                    {
                        Console.WriteLine($"### GPT HUB FAILURE TYPE={hubEx.GetType().FullName} ###");
                        Console.WriteLine($"### GPT HUB FAILURE MESSAGE={hubEx.Message} ###");
                        Console.WriteLine($"### GPT HUB FAILURE STACK={hubEx} ###");

                        _logger.LogError(
                            hubEx,
                            "[GPT-PIPELINE][ASYNC] Failed to send failure notification to hub. InteractionId={InteractionId}, RequestId={RequestId}",
                            interactionId,
                            requestId);
                    }
                }
                finally
                {
                    _gptRequestRegistry.Remove(interactionId);
                    linkedCts.Dispose();

                    _logger.LogInformation(
                        "[GPT-PIPELINE][ASYNC] Cleanup done. InteractionId={InteractionId}, RequestId={RequestId}",
                        interactionId,
                        requestId);
                }
            }, CancellationToken.None);

            return new GptStartResponseDto
            {
                Accepted = true,
                InteractionId = interactionId,
                RequestId = requestId,
                StartedAtUtc = startedAtUtc,
                Status = "started",
                Message = "GPT generation started."
            };
        }

        public async Task<GptInteractionDTO> RunMistralRequestAsync(
            GptPromptRequest request,
            CancellationToken ct = default)
        {
            Console.WriteLine("### GPT ORCHESTRATOR SYNC START CALLED ###");

            ValidateRequest(request);

            var prompt = request.Prompt.Trim();

            _logger.LogInformation(
                "[GPT-PIPELINE][SYNC] ASK-MISTRAL-SYNC started. PromptLength={PromptLength}, Lat={Lat}, Lng={Lng}, HttpTokenCanBeCanceled={HttpTokenCanBeCanceled}, HttpTokenIsCanceled={HttpTokenIsCanceled}",
                prompt.Length,
                request.Latitude,
                request.Longitude,
                ct.CanBeCanceled,
                ct.IsCancellationRequested);

            var created = await CreateInitialInteractionAsync(request, prompt, ct);
            var interactionId = created.Id;

            // Here, in synchronous mode, we can link to the HTTP token + application shutdown
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appLifetime.ApplicationStopping);
            var requestId = _gptRequestRegistry.Register(interactionId, linkedCts);

            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseStarted",
                    interactionId,
                    requestId,
                    CancellationToken.None);

                var finalDto = await ExecutePipelineInternalAsync(
                    request: request,
                    prompt: prompt,
                    interactionId: interactionId,
                    requestId: requestId,
                    ct: linkedCts.Token,
                    pushChunksToHub: false,
                    emitStartedEvent: false);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseChunk",
                    finalDto.Id,
                    requestId,
                    string.Empty,
                    true,
                    CancellationToken.None);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponse",
                    finalDto,
                    CancellationToken.None);

                return finalDto;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[GPT-PIPELINE][SYNC] Request cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseCancelled",
                    interactionId,
                    requestId,
                    CancellationToken.None);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[GPT-PIPELINE][SYNC] Request failed. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    requestId);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveGptResponseFailed",
                    interactionId,
                    ex.Message,
                    CancellationToken.None);

                throw;
            }
            finally
            {
                _gptRequestRegistry.Remove(interactionId);

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
            await using var createScope = _scopeFactory.CreateAsyncScope();
            var gptRepository = createScope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();

            _logger.LogInformation("[GPT-PIPELINE] Persisting initial interaction with empty response...");

            var created = await gptRepository.UpsertInteractionAsync(new GPTInteraction
            {
                Prompt = prompt,
                Response = string.Empty,
                Active = true,
                Model = "mistral",
                Temperature = 0.3f,
                SourceType = "MistralLocal",
                Latitude = request.Latitude,
                Longitude = request.Longitude
            });

            if (created is null || created.Id <= 0)
                throw new InvalidOperationException("Unable to create GPT interaction.");

            _logger.LogInformation(
                "[GPT-PIPELINE] Initial interaction persisted. InteractionId={InteractionId}, PromptHash={PromptHash}, CreatedAt={CreatedAt}, ResponseLength={ResponseLength}",
                created.Id,
                created.PromptHash,
                created.CreatedAt,
                created.Response?.Length ?? 0);

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
                    interactionId,
                    requestId,
                    CancellationToken.None);
            }

            await using var scope = _scopeFactory.CreateAsyncScope();

            var scopedRepository = scope.ServiceProvider.GetRequiredService<IGptInteractionRepository>();
            var localAiContextService = scope.ServiceProvider.GetRequiredService<ILocalAiContextService>();
            var mistralAiService = scope.ServiceProvider.GetRequiredService<IMistralAIService>();

            Console.WriteLine("### BEFORE OLLAMA CALL ###");

            var beforeContext = Stopwatch.StartNew();

            var localContext = await localAiContextService.BuildContextAsync(
                prompt,
                request.Latitude,
                request.Longitude,
                ct);

            beforeContext.Stop();

            _logger.LogInformation(
                "[GPT-PIPELINE] Local context built. InteractionId={InteractionId}, RequestId={RequestId}, ElapsedMs={ElapsedMs}, Events={Events}, CrowdCalendar={CrowdCalendar}, CrowdInfo={CrowdInfo}, Traffic={Traffic}, Weather={Weather}",
                interactionId,
                requestId,
                beforeContext.ElapsedMilliseconds,
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
                groundedPrompt.Substring(0, Math.Min(300, groundedPrompt.Length)));

            Console.WriteLine("### BEFORE NON-STREAM OLLAMA CALL ###");

            string finalResponse;
            if (pushChunksToHub)
            {
                var chunkCount = 0;
                var streamedChars = 0;

                finalResponse = await mistralAiService.StreamFromPromptAsync(
                    groundedPrompt,
                    async chunk =>
                    {
                        chunkCount++;
                        streamedChars += chunk.Chunk?.Length ?? 0;

                        _logger.LogInformation(
                            "[GPT-PIPELINE] Chunk received. InteractionId={InteractionId}, RequestId={RequestId}, ChunkIndex={ChunkIndex}, ChunkLength={ChunkLength}, TotalChars={TotalChars}, IsFinal={IsFinal}",
                            interactionId,
                            requestId,
                            chunkCount,
                            chunk.Chunk?.Length ?? 0,
                            streamedChars,
                            chunk.IsFinal);

                        chunk.InteractionId = interactionId;
                        chunk.RequestId = requestId;

                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveGptResponseChunk",
                            interactionId,
                            requestId,
                            chunk.Chunk,
                            false,
                            CancellationToken.None);
                    },
                    ct);
            }
            else
            {
                finalResponse = await mistralAiService.GenerateFromPromptAsync(
                    groundedPrompt,
                    ct);
            }

            Console.WriteLine($"### AFTER NON-STREAM OLLAMA CALL finalResponseLength={finalResponse?.Length ?? 0} ###");

            if (string.IsNullOrWhiteSpace(finalResponse))
                finalResponse = "No response from Mistral.";

            Console.WriteLine("### BEFORE LOAD EXISTING ###");

            var existing = await scopedRepository.GetByIdAsync(interactionId);

            Console.WriteLine($"### AFTER LOAD EXISTING found={(existing is not null)} ###");

            if (existing is null)
                throw new InvalidOperationException($"GPT interaction {interactionId} not found for final update.");

            existing.Response = finalResponse;
            existing.Active = true;
            existing.Model ??= "mistral";
            existing.Temperature ??= 0.3f;
            existing.SourceType ??= "MistralLocal";

            Console.WriteLine("### BEFORE FINAL UPSERT ###");

            var updated = await scopedRepository.UpsertInteractionAsync(existing);

            Console.WriteLine($"### AFTER FINAL UPSERT updatedNull={(updated is null)} responseLength={updated?.Response?.Length ?? existing.Response?.Length ?? 0} ###");

            var finalDto = (updated ?? existing).MapToGptInteractionDTO();

            _logger.LogInformation(
                "[GPT-PIPELINE] Final interaction persisted. InteractionId={InteractionId}, RequestId={RequestId}, TotalElapsedMs={ElapsedMs}, PersistedResponseLength={PersistedResponseLength}",
                finalDto.Id,
                requestId,
                sw.ElapsedMilliseconds,
                finalDto.Response?.Length ?? 0);

            Console.WriteLine($"### AFTER OLLAMA CALL finalResponseLength={finalResponse?.Length ?? 0} ###");

            return finalDto;
        }
    }
}