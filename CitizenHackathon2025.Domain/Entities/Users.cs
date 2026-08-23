using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public class Users
    {
        public int Id { get; private set; }
        public string Email { get; set; } = string.Empty;

        /*
         * LEGACY SHA512 ONLY.
         *
         * Remove after migration.
         */
        public byte[]? PasswordHash { get; set; }

        /*
         * Current ASP.NET Core Identity hash.
         */
        public string? PasswordHashV2 { get; set; }
        public Guid SecurityStamp { get; set; } = Guid.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public UserStatus Status { get; set; }
        public bool Active { get; private set; } = true;
        public void Activate() => Active = true;
        public void Deactivate() => Active = false;
    }
}

















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.