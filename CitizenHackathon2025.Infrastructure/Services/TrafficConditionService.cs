using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class TrafficConditionService : ITrafficConditionService
    {
    #nullable disable
        private readonly ITrafficConditionRepository _trafficConditionRepository;

        public TrafficConditionService(ITrafficConditionRepository trafficConditionRepository)
        {
            _trafficConditionRepository = trafficConditionRepository;
        }

        public Task<TrafficDTO> CheckRoadAsync(Domain.Entities.ValueObjects.Location from, string to)
        {
            throw new NotImplementedException();
        }

        public Task CheckRoadAsync(Domain.ValueObjects.Location userPosition, string destination)
        {
            throw new NotImplementedException();
        }

        public Task<TrafficAnalysisResult> CheckRoadAsync(string from, string to)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<TrafficCondition>> GetLatestTrafficConditionAsync()
        {
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync();
            return trafficConditions;
        }
        public async Task<TrafficCondition?> GetByIdAsync(int id)
        {
            return await _trafficConditionRepository.GetByIdAsync(id);
        }

        public async Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition trafficCondition)
        {
            return await _trafficConditionRepository.SaveTrafficConditionAsync(trafficCondition);
        }

        public TrafficCondition UpdateTrafficCondition(TrafficCondition trafficCondition)
        {
            try
            {
                var updatedTrafficCondition = _trafficConditionRepository.UpdateTrafficCondition(trafficCondition);
                if (updatedTrafficCondition == null)
                {
                    throw new KeyNotFoundException("Traffic condition not found for update.");
                }
                return updatedTrafficCondition;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                Console.WriteLine($"Validation error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating traffic condition : {ex}");
            }
            return null;
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.