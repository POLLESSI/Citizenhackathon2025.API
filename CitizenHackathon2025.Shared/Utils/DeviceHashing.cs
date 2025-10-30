// File: Shared/Utils/DeviceHashing.cs
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Shared.Utils
{
    public static class DeviceHashing
    {
        public static byte[] ComputeDeviceHash(string rawIdentifier, byte[] pepper)
        {
            if (rawIdentifier is null) throw new ArgumentNullException(nameof(rawIdentifier));
            if (pepper is null || pepper.Length == 0) throw new ArgumentException("Pepper must be provided", nameof(pepper));

            using var hmac = new HMACSHA256(pepper);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(rawIdentifier));
        }
    }
}
