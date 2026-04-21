using CitizenHackathon2025.Application.Interfaces;
using System.Collections.Concurrent;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class GptRequestRegistry : IGptRequestRegistry
    {
        private readonly ConcurrentDictionary<int, ActiveGptRequest> _requests = new();

        public string Register(int interactionId, CancellationTokenSource cts)
        {
            if (interactionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(interactionId));

            ArgumentNullException.ThrowIfNull(cts);

            var requestId = Guid.NewGuid().ToString("N");

            var request = new ActiveGptRequest
            {
                RequestId = requestId,
                CancellationTokenSource = cts
            };

            _requests[interactionId] = request;

            return requestId;
        }

        public bool TryGet(int interactionId, out string requestId, out CancellationTokenSource? cts)
        {
            if (_requests.TryGetValue(interactionId, out var request))
            {
                requestId = request.RequestId;
                cts = request.CancellationTokenSource;
                return true;
            }

            requestId = string.Empty;
            cts = null;
            return false;
        }

        public bool TryCancel(int interactionId, string? requestId = null)
        {
            if (!_requests.TryGetValue(interactionId, out var request))
                return false;

            if (!string.IsNullOrWhiteSpace(requestId) &&
                !string.Equals(request.RequestId, requestId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!request.CancellationTokenSource.IsCancellationRequested)
                request.CancellationTokenSource.Cancel();

            return true;
        }

        public void Remove(int interactionId, string? requestId = null)
        {
            if (!_requests.TryGetValue(interactionId, out var request))
                return;

            if (!string.IsNullOrWhiteSpace(requestId) &&
                !string.Equals(request.RequestId, requestId, StringComparison.Ordinal))
            {
                return;
            }

            _requests.TryRemove(interactionId, out _);
        }
    }
}































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.