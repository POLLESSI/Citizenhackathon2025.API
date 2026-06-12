using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Options;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Contracts.Hubs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Extensions;
using CitizenHackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;


namespace CitizenHackathon2025.Infrastructure.Services
{
    public class CrowdInfoService : ICrowdInfoService
    {
    #nullable disable
        private readonly ICrowdInfoRepository _crowdInfoRepository;
        private readonly ICriticalAlertQuorumService _criticalAlertQuorumService;
        private readonly IPlaceRepository _placeRepository;
        private readonly ICrowdAlertVoteRepository _crowdAlertVoteRepository;
        private readonly CriticalAlertRules _rules;
        private readonly HttpClient _http;
        private readonly IHubContext<CrowdHub> _crowdHubContext;

        public CrowdInfoService(ICrowdInfoRepository crowdInfoRepository, ICriticalAlertQuorumService criticalAlertQuorumService, IPlaceRepository placeRepository, IHttpClientFactory httpClientFactory, IOptions<CriticalAlertRules> options, IHubContext<CrowdHub> crowdHubContext)
        {
            _crowdInfoRepository = crowdInfoRepository;
            _criticalAlertQuorumService = criticalAlertQuorumService;
            _placeRepository = placeRepository;
            _rules = options.Value;
            _http = httpClientFactory.CreateClient("ApiWithAuth");
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
            var saved = await _crowdInfoRepository.UpsertCrowdInfoAsync(crowdInfo, ct);

            if (saved != null)
            {
                await _crowdHubContext.BroadcastCrowdUpdate(saved);
                await _crowdHubContext.BroadcastCrowdRefreshRequested("sync"); 
            }
            return saved!;
        }

        public async Task<ManualCriticalAlertResultDTO> CreateManualCriticalAlertAsync(CitizenHackathon2025.Contracts.DTOs.ManualCrowdCriticalAlertRequest request, CancellationToken ct = default)
        {
            if (request.PlaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.PlaceId));

            var place = await _placeRepository.GetByIdAsync(request.PlaceId, ct);

            if (place is null)
            {
                return new ManualCriticalAlertResultDTO
                {
                    Ok = false,
                    Status = "Error",
                    Error = $"Place {request.PlaceId} not found."
                };
            }

            var quorum = await _criticalAlertQuorumService.RegisterVoteAsync(
                CriticalAlertKind.Crowd,
                request.PlaceId,
                place.Latitude,
                place.Longitude,
                request.DeviceId,
                request.Reason,
                ct);

            if (!quorum.Confirmed)
            {
                return new ManualCriticalAlertResultDTO
                {
                    Ok = true,
                    Status = "Pending",
                    ConfirmationCount = quorum.ConfirmationCount,
                    RequiredCount = quorum.RequiredCount
                };
            }

            var alert = await _crowdInfoRepository.CreateManualCriticalAlertAsync(
                request.PlaceId,
                request.Reason,
                request.Source ?? "ManualButton",
                ct);

            await _crowdHubContext.Clients.All.SendAsync(
                CrowdHubMethods.ToClient.ReceiveCrowdUpdate,
                alert,
                ct);

            return new ManualCriticalAlertResultDTO
            {
                Ok = true,
                Status = "Confirmed",
                ConfirmationCount = quorum.ConfirmationCount,
                RequiredCount = quorum.RequiredCount,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_rules.AlertDurationMinutes),
                CrowdInfoId = alert.Id
            };
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

        public Task<CrowdInfo> UpsertCrowdInfoAsync(CrowdInfo input, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        private static string BuildZoneKey(decimal latitude, decimal longitude)
        {
            var latBucket = Math.Round(latitude, 3);
            var lngBucket = Math.Round(longitude, 3);

            return $"{latBucket:0.000}:{lngBucket:0.000}";
        }

        private static string HashText(string value)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(value.Trim()));

            return Convert.ToHexString(bytes);
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.