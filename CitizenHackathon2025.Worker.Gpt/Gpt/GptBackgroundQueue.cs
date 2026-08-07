using CitizenHackathon2025.Application.Gpt;
using CitizenHackathon2025.Application.Interfaces;
using System.Threading.Channels;

namespace CitizenHackathon2025.Worker.Gpt
{
    public sealed class GptBackgroundQueue : IGptBackgroundQueue
    {
        private readonly Channel<GptWorkItem> _queue;

        public GptBackgroundQueue() : this(capacity: 16)
        {
        }

        public GptBackgroundQueue(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            var options = new BoundedChannelOptions(capacity)
            {
                FullMode =
                    BoundedChannelFullMode.Wait,

                SingleReader = true,
                SingleWriter = false,

                AllowSynchronousContinuations =
                    false
            };

            _queue = Channel.CreateBounded<GptWorkItem>(options);
        }

        public ValueTask QueueAsync(GptWorkItem workItem, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(workItem);

            return _queue.Writer.WriteAsync(workItem, cancellationToken);
        }

        public ValueTask<GptWorkItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}





























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.