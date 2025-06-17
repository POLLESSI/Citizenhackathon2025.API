using System;

namespace Citizenhackathon2025.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; }
        public string Email { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.