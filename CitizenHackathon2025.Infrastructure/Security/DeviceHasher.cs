using CitizenHackathon2025.Shared.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Security
{
    public class DeviceHasher : IDeviceHasher
    {
        private readonly byte[] _pepper;
        public DeviceHasher(IOptions<DeviceHasherOptions> opts)
        {
            if (opts?.Value == null) throw new ArgumentNullException(nameof(opts));
            if (string.IsNullOrEmpty(opts.Value.PepperBase64)) throw new InvalidOperationException("Pepper not configured");
            _pepper = Convert.FromBase64String(opts.Value.PepperBase64);
        }
        public byte[] ComputeHash(string rawIdentifier)
        {
            if (rawIdentifier is null) throw new ArgumentNullException(nameof(rawIdentifier));
            using var hmac = new HMACSHA256(_pepper);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(rawIdentifier));
        }

        public string ComputeHashBase64(string rawIdentifier)
        {
            return Convert.ToBase64String(ComputeHash(rawIdentifier));
        }
    }
}
