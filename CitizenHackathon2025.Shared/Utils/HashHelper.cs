using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Shared.Utils
{
    public static class HashHelper
    {
        public static string GetSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2")); // hexadécimal en minuscules
            return sb.ToString();
        }
    }
}