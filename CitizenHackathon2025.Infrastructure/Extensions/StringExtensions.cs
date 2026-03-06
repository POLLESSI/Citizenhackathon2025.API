using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static string Hash(this string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
