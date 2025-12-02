using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class UserSession
    {
        public long Id { get; set; }              // DB identity
        public string UserEmail { get; set; } = string.Empty;
        public string Jti { get; set; } = string.Empty; // JWT ID (Guid string)
        public Guid? RefreshFamilyId { get; set; }            // optional
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public SessionSource Source { get; set; } = SessionSource.Api;
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public bool IsRevoked { get; set; } = false;

        public bool IsExpired() => ExpiresAtUtc <= DateTime.UtcNow;
        public bool IsActive() => !IsRevoked && !IsExpired();
    }
}






































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.