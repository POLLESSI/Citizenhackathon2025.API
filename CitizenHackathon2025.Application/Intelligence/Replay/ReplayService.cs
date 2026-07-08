using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Intelligence.Replay
{
    public sealed class ReplayService : IReplayService
    {
        public Task<List<ReplayFrameDTO>> GetFramesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            if (fromUtc >= toUtc)
                throw new ArgumentException("Replay start date must be earlier than end date.");

            // For now: no frames in the database.
            // We return an empty list cleanly.
            return Task.FromResult(new List<ReplayFrameDTO>());
        }

        public Task<ReplaySession> StartReplayAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            if (fromUtc >= toUtc)
                throw new ArgumentException("Replay start date must be earlier than end date.");

            return Task.FromResult(new ReplaySession
            {
                Id = Guid.NewGuid(),
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Status = "Created",
                FrameCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }
}



































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.