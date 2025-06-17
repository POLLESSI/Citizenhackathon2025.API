using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync()
        {
            var crowdInfos = await _crowdInfoRepository.GetAllCrowdInfoAsync();
            return crowdInfos;
        }

        public Task<CrowdInfo> GetCrowdInfoByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("The crowd info ID must be greater than zero.", nameof(id));
            }
            var crowdInfo = _crowdInfoRepository.GetCrowdInfoByIdAsync(id);
            if (crowdInfo == null)
            {
                return null;
            }

            return crowdInfo;
        }

        public async Task<CrowdInfo> SaveCrowdInfoAsync(CrowdInfo crowdInfo)
        {
            return await _crowdInfoRepository.SaveCrowdInfoAsync(crowdInfo);
        }

        public CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo)
        {
            try
            {
                var UpdateCrowdInfo = _crowdInfoRepository.UpdateCrowdInfo(crowdInfo);
                if (UpdateCrowdInfo == null)
                {
                    throw new ArgumentException("The event to update cannot be null.", nameof(crowdInfo));
                }
                return UpdateCrowdInfo;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Crowd Info : {ex}");
            }
            return null;
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.