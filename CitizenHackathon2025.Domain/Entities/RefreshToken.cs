using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public class RefreshToken
    {
#nullable disable
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty; // returned to the client, but no longer stored in DB (optional)
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public RefreshTokenStatus Status { get; set; } = RefreshTokenStatus.Active;
        public bool Active { get; set; } = true;
        public byte[] TokenHash { get; set; } = Array.Empty<byte>();
        public byte[] TokenSalt { get; set; } = Array.Empty<byte>();

        public bool IsActive() => Status == RefreshTokenStatus.Active && ExpiryDate > DateTime.UtcNow;
        public void Revoke() => Status = RefreshTokenStatus.Revoked;
        public void Expire() => Status = RefreshTokenStatus.Expired;
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.