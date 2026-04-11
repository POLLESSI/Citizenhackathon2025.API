using CitizenHackathon2025.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GptRequestRegistry : IGptRequestRegistry
    {
        private sealed class ActiveRequest
        {
            public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
            public CancellationTokenSource CancellationTokenSource { get; init; } = default!;
            public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        }

        private readonly ConcurrentDictionary<int, ActiveRequest> _requests = new();
        private readonly ILogger<GptRequestRegistry> _logger;

        public GptRequestRegistry(ILogger<GptRequestRegistry> logger)
        {
            _logger = logger;
        }

        public string Register(int interactionId, CancellationTokenSource cts)
        {
            if (interactionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(interactionId));

            ArgumentNullException.ThrowIfNull(cts);

            var request = new ActiveRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                CancellationTokenSource = cts,
                CreatedUtc = DateTime.UtcNow
            };

            if (_requests.TryRemove(interactionId, out var previous))
            {
                try
                {
                    previous.CancellationTokenSource.Cancel();
                }
                catch { }

                previous.CancellationTokenSource.Dispose();
            }

            _requests[interactionId] = request;

            _logger.LogInformation(
                "GPT request registered. InteractionId={InteractionId}, RequestId={RequestId}",
                interactionId,
                request.RequestId);

            return request.RequestId;
        }

        public bool TryCancel(int interactionId, string? requestId)
        {
            if (!_requests.TryGetValue(interactionId, out var entry))
                return false;

            if (!string.IsNullOrWhiteSpace(requestId) &&
                !string.Equals(entry.RequestId, requestId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "GPT cancel rejected due to requestId mismatch. InteractionId={InteractionId}, Expected={Expected}, Actual={Actual}",
                    interactionId,
                    entry.RequestId,
                    requestId);

                return false;
            }

            try
            {
                entry.CancellationTokenSource.Cancel();

                _logger.LogInformation(
                    "GPT request cancelled. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    entry.RequestId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while cancelling GPT request. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    entry.RequestId);

                return false;
            }
        }

        public bool TryGet(int interactionId, out CancellationTokenSource? cts)
        {
            if (_requests.TryGetValue(interactionId, out var entry))
            {
                cts = entry.CancellationTokenSource;
                return true;
            }

            cts = null;
            return false;
        }

        public void Remove(int interactionId)
        {
            if (_requests.TryRemove(interactionId, out var entry))
            {
                try
                {
                    entry.CancellationTokenSource.Dispose();
                }
                catch { }

                _logger.LogInformation(
                    "GPT request removed. InteractionId={InteractionId}, RequestId={RequestId}",
                    interactionId,
                    entry.RequestId);
            }
        }
    }
}































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.