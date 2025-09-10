using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using System.Net.Http.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class CrowdInfoService : ICrowdInfoService
    {
    #nullable disable
        private readonly ICrowdInfoRepository _crowdInfoRepository;
        private readonly HttpClient _http;

        public CrowdInfoService(ICrowdInfoRepository crowdInfoRepository, IHttpClientFactory f)
        {
            _crowdInfoRepository = crowdInfoRepository;
            _http = f.CreateClient("ApiWithAuth");
        }
        public async Task<List<CrowdInfoDTO>> GetAllAsync()
            => (await _http.GetFromJsonAsync<List<CrowdInfoDTO>>("crowdinfo/all")) ?? new();

        public async Task<CrowdInfoDTO?> GetByIdAsync(int id)
            => await _http.GetFromJsonAsync<CrowdInfoDTO>($"crowdinfo/{id}");

        public async Task<CrowdInfoDTO?> SaveAsync(CrowdInfoDTO dto)
        {
            var res = await _http.PostAsJsonAsync("crowdinfo", dto);
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<CrowdInfoDTO>() : null;
        }

        public async Task<bool> ArchiveAsync(int id)
            => (await _http.DeleteAsync($"crowdinfo/archive/{id}")).IsSuccessStatusCode;

        // ==== Interface ICrowdInfoService (avec tokens) ====

        public Task<bool> DeleteCrowdInfoAsync(int id, CancellationToken ct = default)
            => _crowdInfoRepository.DeleteCrowdInfoAsync(id); // repo sans ct → on ignore ct ici

        public async Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(CancellationToken ct = default)
            => await _crowdInfoRepository.GetAllCrowdInfoAsync();

        public async Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id, CancellationToken ct = default)
            => await _crowdInfoRepository.GetCrowdInfoByIdAsync(id);

        public Task<CrowdLevelDTO> GetCrowdLevelAsync(string destination, CancellationToken ct = default)
            => throw new NotImplementedException();

        public async Task<CrowdInfo> SaveCrowdInfoAsync(CrowdInfo crowdInfo, CancellationToken ct = default)
            => await _crowdInfoRepository.SaveCrowdInfoAsync(crowdInfo);

        public CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo)
        {
            try
            {
                var updated = _crowdInfoRepository.UpdateCrowdInfo(crowdInfo);
                if (updated is null)
                    throw new ArgumentException("The event to update cannot be null.", nameof(crowdInfo));
                return updated;
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