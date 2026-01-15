using Azure;
using Azure.Security.KeyVault.Secrets;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.API.Azure.Security.KeyVault
{
    public class SecretClientWrapper : ISecretClientWrapper
    {
        private readonly SecretClient _client;
        public SecretClientWrapper(SecretClient client) => _client = client;
        public Task<Response<KeyVaultSecret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
            => _client.GetSecretAsync(name, cancellationToken: cancellationToken);
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.