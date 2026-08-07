using CitizenHackathon2025.Application.Gpt;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGptBackgroundQueue
    {
        ValueTask QueueAsync(GptWorkItem workItem, CancellationToken cancellationToken = default);
        ValueTask<GptWorkItem> DequeueAsync(CancellationToken cancellationToken);
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.