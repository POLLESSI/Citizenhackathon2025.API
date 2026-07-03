namespace CitizenHackathon2025.Application.Intelligence.Replay
{
    public interface IReplayService
    {
        Task<ReplaySession> StartReplayAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default);
    }

    public sealed class ReplaySession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public int SnapshotCount { get; set; }
        public string Status { get; set; } = "Created";
    }
}













































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.