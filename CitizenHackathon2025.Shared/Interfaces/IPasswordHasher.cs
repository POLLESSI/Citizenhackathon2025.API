
namespace CitizenHackathon2025.Shared.Interfaces
{
    public interface IPasswordHasher
    {
        byte[] HashPassword(string password, string securityStamp);
    }
}
