using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Services
{
    public class CrowdInfoService : ICrowdInfoService
    {
    #nullable disable
        private readonly ICrowdInfoRepository _crowdInfoRepository;

        public CrowdInfoService(ICrowdInfoRepository crowdInfoRepository)
        {
            _crowdInfoRepository = crowdInfoRepository;
        }

        public Task<bool> DeleteCrowdInfoAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync()
        {
            throw new NotImplementedException();
        }

        public Task<CrowdInfo> GetCrowdInfoByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<CrowdInfo> SaveCrowdInfoAsync(CrowdInfo crowdInfo)
        {
            throw new NotImplementedException();
        }
    }
}
