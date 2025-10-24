namespace CitizenHackathon2025.Domain.Queries
{
    public sealed class SessionQuery
    {
        public string? Email { get; init; }
        public bool OnlyActive { get; init; } = true;
        public string? Jti { get; init; }
        public int Take { get; init; } = 200;
        public int Skip { get; init; } = 0;
        public DateTime? FromUtc { get; init; }
        public DateTime? ToUtc { get; init; }
    }
}
