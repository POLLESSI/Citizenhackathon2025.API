using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public class RefreshToken
    {
#nullable disable
        public int Id { get; set; }                  
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public RefreshTokenStatus Status { get; set; } = RefreshTokenStatus.Active;
        public bool IsActive() => Status == RefreshTokenStatus.Active && ExpiryDate > DateTime.UtcNow;
        public void Revoke() => Status = RefreshTokenStatus.Revoked;
        public void Expire() => Status = RefreshTokenStatus.Expired;
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.