namespace Citizenhackathon2025.Domain.Entities
{
    public class User
    {
        public int Id { get; private set; }
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public string Role { get; set; } = "User";
        public bool Active { get; private set; } = true;
    }
}
