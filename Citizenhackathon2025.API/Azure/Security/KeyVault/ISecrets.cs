namespace CitizenHackathon2025.API.Azure.Security.KeyVault
{
    /// <summary>
    /// Testable interface for KeyVault secrets wrapper.
    /// </summary>
    public interface ISecrets
    {
        /// <summary>Retrieves the value of the secret (string). May come from the cache.</summary>
        Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);

        /// <summary>Retrieves the value of the secret decoded in bytes from a Base64 (useful for pepper).</summary>
        Task<byte[]> GetSecretBytesAsync(string secretName, CancellationToken ct = default);

        /// <summary>Retrieves the pepper (byte[]) stored under the secret "device-pepper"; handy helper.</summary>
        Task<byte[]> GetDevicePepperAsync(CancellationToken ct = default);
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.