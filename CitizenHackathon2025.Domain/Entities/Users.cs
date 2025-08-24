using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public class Users
    {
        public int Id { get; private set; }
        public string Email { get; set; } = string.Empty;
        public Guid SecurityStamp { get; set; } = Guid.Empty;
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public UserRole Role { get; set; } = UserRole.User; // ✅ enum instead of string
        public UserStatus Status { get; set; } // Dapper automatically maps the DB int
        public bool Active { get; private set; } = true;
        public void Activate() => Active = true;
        public void Deactivate() => Active = false;

    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.