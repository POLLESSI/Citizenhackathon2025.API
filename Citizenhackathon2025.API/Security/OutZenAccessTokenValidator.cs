using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.API.Security
{
    public static class OutZenAccessTokenValidator
    {
        private const string SecretKey = "OUTZEN_SECRET_KEY_SUPER_SAFE"; // stocké en config dans appsettings

        public static bool Validate(string token, out string eventId)
        {
            eventId = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var parts = token.Split('|');
            if (parts.Length != 3)
                return false;

            eventId = parts[0];
            if (!long.TryParse(parts[1], out var expiresAtUnix))
                return false;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;
            if (expiresAt < DateTime.UtcNow)
                return false; // Token expiré

            var expectedSignature = ComputeSignature(parts[0], parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature),
                    Encoding.UTF8.GetBytes(parts[2])))
            {
                return false;
            }

            return true;
        }

        public static string Generate(string eventId, TimeSpan validFor)
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
            var signature = ComputeSignature(eventId, expiresAt.ToString());
            return $"{eventId}|{expiresAt}|{signature}";
        }

        private static string ComputeSignature(string eventId, string expiresAt)
        {
            var secretBytes = Encoding.UTF8.GetBytes(SecretKey);
            var message = Encoding.UTF8.GetBytes($"{eventId}|{expiresAt}");

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(message);
            return Convert.ToHexString(hash);
        }
    }
}
