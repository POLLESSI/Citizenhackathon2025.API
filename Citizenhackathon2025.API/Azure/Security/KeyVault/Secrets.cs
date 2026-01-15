using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace CitizenHackathon2025.API.Azure.Security.KeyVault
{
    /// <summary>
    /// Simple but robust implementation: SecretClient + MemoryCache + logging + TTL.
    /// Usage: Record in Singleton. Allows rotation (TTL) and avoids KeyVault calls for each request.
    /// </summary>
    public class Secrets : ISecrets, IDisposable
    {
    #nullable disable
        private readonly SecretClient _client;
        private readonly IMemoryCache _cache;
        private readonly ILogger<Secrets> _logger;
        private readonly TimeSpan _cacheTtl;
        private bool _disposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Azure Key Vault SecretClient (injected)</param>
        /// <param name="memoryCache">IMemoryCache (injected)</param>
        /// <param name="logger">ILogger (injected)</param>
        /// <param name="cacheTtl">TTL cache (default 5 minutes)</param>
        public Secrets(SecretClient client, IMemoryCache memoryCache, ILogger<Secrets> logger, TimeSpan? cacheTtl = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
        }

        /// <inheritdoc />
        public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(secretName)) throw new ArgumentException("secretName required", nameof(secretName));
            var cacheKey = $"kv:{secretName}";

            // Use GetOrCreateAsync to avoid multiple simultaneous fetches
            return await _cache.GetOrCreateAsync<string>(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
                entry.Priority = CacheItemPriority.Normal;

                _logger.LogDebug("Cache miss for '{SecretName}', fetching from KeyVault.", secretName);

                try
                {
                    Response<KeyVaultSecret> resp = await _client.GetSecretAsync(secretName, cancellationToken: ct).ConfigureAwait(false);
                    var value = resp.Value?.Value ?? string.Empty;
                    return value;
                }
                catch (RequestFailedException rfe) when (rfe.Status == 404 || rfe.Status == 403)
                {
                    // Decision: do NOT cache missing/forbidden indefinitely; but we may cache a sentinel short TTL to reduce noise.
                    _logger.LogWarning(rfe, "KeyVault returned {Status} for secret '{SecretName}'.", rfe.Status, secretName);
                    // Option: cache an empty string for a short period to avoid hammering KeyVault on repeated invalid names
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                    return string.Empty;
                }
            }).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<byte[]> GetSecretBytesAsync(string secretName, CancellationToken ct = default)
        {
            var str = await GetSecretAsync(secretName, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(str)) return Array.Empty<byte>();

            try
            {
                return Convert.FromBase64String(str);
            }
            catch (FormatException)
            {
                _logger.LogWarning("Secret '{SecretName}' is not Base64; returning UTF8 bytes instead.", secretName);
                return Encoding.UTF8.GetBytes(str);
            }
        }

        /// <inheritdoc />
        public Task<byte[]> GetDevicePepperAsync(CancellationToken ct = default)
        {
            // secret name used in earlier examples: "device-pepper"
            return GetSecretBytesAsync("device-pepper", ct);
        }
        public void EvictSecret(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName)) return;
            _cache.Remove($"kv:{secretName}");
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Nothing to dispose for SecretClient / IMemoryCache; kept for pattern completeness.
        }
    }
}













































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.