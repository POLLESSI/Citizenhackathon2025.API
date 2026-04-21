namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class ActiveGptRequest
    {
        public string RequestId { get; init; } = string.Empty;
        public CancellationTokenSource CancellationTokenSource { get; init; } = default!;
    }
}