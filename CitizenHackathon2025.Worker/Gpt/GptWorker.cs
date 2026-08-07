using CitizenHackathon2025.Application.Gpt;
using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Worker.Gpt
{
    public sealed class GptWorker : BackgroundService
    {
        private readonly IGptBackgroundQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GptWorker> _logger;

        public GptWorker(IGptBackgroundQueue queue, IServiceScopeFactory scopeFactory, ILogger<GptWorker> logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[GPT-WORKER] Started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var workItem = await _queue.DequeueAsync(stoppingToken);
                    await ProcessAsync(workItem, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[GPT-WORKER] Fatal worker failure.");

                throw;
            }
            finally
            {
                _logger.LogInformation("[GPT-WORKER] Stopped.");
            }
        }

        private async Task ProcessAsync(GptWorkItem workItem, CancellationToken stoppingToken)
        {
            _logger.LogInformation("[GPT-WORKER] Processing started. " + "InteractionId={InteractionId}, " + "RequestId={RequestId}", workItem.Interaction.Id, workItem.RequestId);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var processor = scope.ServiceProvider.GetRequiredService<IGptQueuedRequestProcessor>();

                await processor.ProcessQueuedAsync(workItem, stoppingToken);

                _logger.LogInformation("[GPT-WORKER] Processing finished. " + "InteractionId={InteractionId}, " + "RequestId={RequestId}", workItem.Interaction.Id, workItem.RequestId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("[GPT-WORKER] Processing interrupted " + "because application is stopping. " + "InteractionId={InteractionId}", workItem.Interaction.Id);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GPT-WORKER] Unexpected processing " + "failure. " + "InteractionId={InteractionId}, " + "RequestId={RequestId}", workItem.Interaction.Id, workItem.RequestId);
            }
        }
    }
}



































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.