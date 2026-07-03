namespace CitizenHackathon2025.Application.Intelligence.Replay
{
    public sealed class ReplayService : IReplayService
    {
        public Task<ReplaySession> StartReplayAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default)
        {
            if (fromUtc >= toUtc)
                throw new ArgumentException("Replay start date must be earlier than end date.");

            return Task.FromResult(new ReplaySession
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                SnapshotCount = 0,
                Status = "Created"
            });
        }
    }
}



































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.