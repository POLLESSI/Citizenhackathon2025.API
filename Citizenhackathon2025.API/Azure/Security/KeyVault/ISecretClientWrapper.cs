using Azure;
using Azure.Security.KeyVault.Secrets;
using System.Threading;
using System.Threading.Tasks;

namespace CitizenHackathon2025.API.Azure.Security.KeyVault
{
    public interface ISecretClientWrapper
    {
        Task<Response<KeyVaultSecret>> GetSecretAsync(string name, CancellationToken cancellationToken = default);
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.