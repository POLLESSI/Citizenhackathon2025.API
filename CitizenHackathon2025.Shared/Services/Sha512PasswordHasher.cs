using CitizenHackathon2025.Shared.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Shared.Services
{
    public class Sha512PasswordHasher : IPasswordHasher
    {
        public byte[] HashPassword(string password, string securityStamp)
        {
            using var sha = SHA512.Create();
            var combined = $"{password}:{securityStamp}";
            return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        }
    }
}


































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.