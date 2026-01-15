using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using CitizenHackathon2025.API.Azure.Security.KeyVault;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CitizenHackathon2025.API.Tests
{
    public class SecretsTests
    {
    #nullable disable
        // Helper: create a mocked Response<KeyVaultSecret>
        private static Mock<Response<KeyVaultSecret>> CreateKeyVaultSecretResponse(string name, string value, int status = 200)
        {
            var kvSecret = new KeyVaultSecret(name, value);

            var mockResponse = new Mock<Response<KeyVaultSecret>>(MockBehavior.Strict);
            mockResponse.Setup(r => r.Value).Returns(kvSecret);

            // Mock the underlying HttpResponse (Azure.Core.HttpResponse is abstract)
            var mockRaw = new Mock<Response>(MockBehavior.Loose);
            // When GetRawResponse is called on the Response<T>, return a mocked raw Response
            mockResponse.Setup(r => r.GetRawResponse()).Returns((Response)mockRaw.Object);

            return mockResponse;
        }

        [Fact]
        public async Task GetSecretAsync_FetchesFromClient_AndCaches()
        {
            // Arrange
            var secretName = "my-secret";
            var secretValue = "super-secret-value";

            var mockClient = new Mock<ISecretClientWrapper>(MockBehavior.Strict);
            var mockedResponse = CreateKeyVaultSecretResponse(secretName, secretValue);

            // Setup wrapper to return the mocked Response<KeyVaultSecret>
            mockClient
                .Setup(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockedResponse.Object)
                .Verifiable();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<Secrets>.Instance;

            var sut = new SecretsAdapterForTest(mockClient.Object, memoryCache, logger, TimeSpan.FromMinutes(5));

            // Act: first call -> should hit client
            var v1 = await sut.GetSecretAsync(secretName);
            // Act: second call -> should come from cache (client not called again)
            var v2 = await sut.GetSecretAsync(secretName);

            // Assert
            Assert.Equal(secretValue, v1);
            Assert.Equal(secretValue, v2);

            // Verify GetSecretAsync on client was called exactly once
            mockClient.Verify(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretBytesAsync_ReturnsBase64Decoded_WhenBase64()
        {
            // Arrange
            var secretName = "pepper";
            var rawBytes = new byte[] { 1, 2, 3, 4, 5 };
            var base64 = Convert.ToBase64String(rawBytes);

            var mockClient = new Mock<ISecretClientWrapper>(MockBehavior.Strict);
            var mockedResponse = CreateKeyVaultSecretResponse(secretName, base64);
            mockClient.Setup(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(mockedResponse.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<Secrets>.Instance;
            var sut = new SecretsAdapterForTest(mockClient.Object, memoryCache, logger);

            // Act
            var res = await sut.GetSecretBytesAsync(secretName);

            // Assert
            Assert.Equal(rawBytes, res);
        }

        [Fact]
        public async Task GetSecretBytesAsync_ReturnsUtf8Bytes_WhenNotBase64()
        {
            // Arrange
            var secretName = "pepper";
            var plain = "not-base64-çé";
            var expected = System.Text.Encoding.UTF8.GetBytes(plain);

            var mockClient = new Mock<ISecretClientWrapper>(MockBehavior.Strict);
            var mockedResponse = CreateKeyVaultSecretResponse(secretName, plain);
            mockClient.Setup(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(mockedResponse.Object);

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<Secrets>.Instance;
            var sut = new SecretsAdapterForTest(mockClient.Object, memoryCache, logger);

            // Act
            var res = await sut.GetSecretBytesAsync(secretName);

            // Assert
            Assert.Equal(expected, res);
        }

        // Minimal adapter to test the logic from your Secrets class but wired to ISecretClientWrapper
        private class SecretsAdapterForTest
        {
            private readonly ISecretClientWrapper _client;
            private readonly IMemoryCache _cache;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly TimeSpan _cacheTtl;

            public SecretsAdapterForTest(ISecretClientWrapper client, IMemoryCache cache, Microsoft.Extensions.Logging.ILogger logger, TimeSpan? cacheTtl = null)
            {
                _client = client;
                _cache = cache;
                _logger = logger;
                _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
            }

            public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(secretName)) throw new ArgumentException("secretName required", nameof(secretName));
                var cacheKey = $"kv:{secretName}";

                return await _cache.GetOrCreateAsync<string>(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
                    var resp = await _client.GetSecretAsync(secretName, ct).ConfigureAwait(false);
                    return resp.Value?.Value ?? string.Empty;
                }).ConfigureAwait(false);
            }

            public async Task<byte[]> GetSecretBytesAsync(string secretName, CancellationToken ct = default)
            {
                var str = await GetSecretAsync(secretName, ct).ConfigureAwait(false);
                if (string.IsNullOrEmpty(str)) return Array.Empty<byte>();
                try { return Convert.FromBase64String(str); }
                catch (FormatException) { return System.Text.Encoding.UTF8.GetBytes(str); }
            }
        }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.