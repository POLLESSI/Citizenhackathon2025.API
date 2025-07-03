using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public static class Hasher
    {
        public static byte[] ComputeHash(string password)
        {
            using var sha512 = SHA512.Create();
            return sha512.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.