using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Extensions;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class CrowdInfoService : ICrowdInfoService
    {
    #nullable disable
        private readonly ICrowdInfoRepository _crowdInfoRepository;
        private readonly HttpClient _http;
        private readonly IHubContext<CrowdHub> _crowdHubContext;

        public CrowdInfoService(ICrowdInfoRepository crowdInfoRepository, IHttpClientFactory f, IHubContext<CrowdHub> crowdHubContext)
        {
            _crowdInfoRepository = crowdInfoRepository;
            _http = f.CreateClient("ApiWithAuth");
            _crowdHubContext = crowdHubContext;
        }

        public async Task<List<CrowdInfoDTO>> GetAllAsync()
            => (await _http.GetFromJsonAsync<List<CrowdInfoDTO>>("crowdinfo/all")) ?? new();

        public async Task<CrowdInfoDTO?> GetByIdAsync(int id)
            => await _http.GetFromJsonAsync<CrowdInfoDTO>($"crowdinfo/{id}");

        public async Task<CrowdInfoDTO?> SaveAsync(CrowdInfoDTO dto)
        {
            var res = await _http.PostAsJsonAsync("crowdinfo", dto);
            // NO broadcast here: API must broadcast via CrowdHub
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<CrowdInfoDTO>() : null;
        }

        public async Task<bool> ArchiveAsync(int id)
        {
            var ok = (await _http.DeleteAsync($"crowdinfo/archive/{id}")).IsSuccessStatusCode;
            // NO broadcast here: API must emit CrowdInfoArchived
            return ok;
        }

        public async Task<bool> DeleteCrowdInfoAsync(int id, CancellationToken ct = default)
        {
            var ok = await _crowdInfoRepository.DeleteCrowdInfoAsync(id);
            if (ok)
            {
                await _crowdHubContext.BroadcastCrowdArchived(id);
            }
            return ok;
        }
        public async Task<IEnumerable<CrowdInfo>> GetAllCrowdInfoAsync(int limit = 200, CancellationToken ct = default)
            => await _crowdInfoRepository.GetAllCrowdInfoAsync();    

        public async Task<CrowdInfo?> GetCrowdInfoByIdAsync(int id, CancellationToken ct = default)
            => await _crowdInfoRepository.GetCrowdInfoByIdAsync(id);

        public Task<CrowdLevelDTO> GetCrowdLevelAsync(string destination, CancellationToken ct = default)
            => throw new NotImplementedException();

        public async Task<CrowdInfo> SaveCrowdInfoAsync(CrowdInfo crowdInfo, CancellationToken ct = default)
        {
            var saved = await _crowdInfoRepository.SaveCrowdInfoAsync(crowdInfo);

            if (saved != null)
            {
                // Full DTO push to clients
                await _crowdHubContext.BroadcastCrowdUpdate(saved);
                // Optional: generic ping
                await _crowdHubContext.BroadcastCrowdRefreshRequested("sync");
            }

            return saved;
        }

        public CrowdInfo UpdateCrowdInfo(CrowdInfo crowdInfo)
        {
            try
            {
                var updated = _crowdInfoRepository.UpdateCrowdInfo(crowdInfo);
                if (updated is null)
                    throw new ArgumentException("The event to update cannot be null.", nameof(crowdInfo));

                // Release of the update
                _ = _crowdHubContext.BroadcastCrowdUpdate(updated);
                return updated;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Crowd Info : {ex}");
                return null;
            }
        }

        public async Task<int> ArchivePastCrowdInfosAsync()
        {
            string sql = "UPDATE CrowdInfo SET Active = 0 WHERE Timestamp < @Threshold AND Active = 1";
            var parameters = new { Threshold = DateTime.UtcNow.Date.AddDays(-2) };
            return await _crowdInfoRepository.ArchivePastCrowdInfosAsync();
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.