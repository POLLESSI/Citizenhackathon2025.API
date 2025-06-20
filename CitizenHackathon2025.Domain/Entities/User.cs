using Citizenhackathon2025.Domain.Enums;

namespace Citizenhackathon2025.Domain.Entities
{
    public class User
    {
        public int Id { get; private set; }
        public string Email { get; set; } = string.Empty;
        public string SecurityStamp { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public Role Role { get; set; } = Role.User; // ✅ enum instead of string
        public Status Status { get; set; } // Dapper automatically maps the DB int
        public bool Active { get; private set; } = true;
        public void Activate() => Active = true;
        public void Deactivate() => Active = false;

    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.