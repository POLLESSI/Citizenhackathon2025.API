using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Application.Services
{
    public class TrafficConditionService : ITrafficConditionService
    {
#nullable disable
        private readonly ITrafficConditionRepository _trafficConditionRepository;

        public TrafficConditionService(ITrafficConditionRepository trafficConditionRepository)
        {
            _trafficConditionRepository = trafficConditionRepository;
        }

        public async Task<IEnumerable<TrafficCondition?>> GetLatestTrafficConditionAsync()
        {
            var trafficConditions = await _trafficConditionRepository.GetLatestTrafficConditionAsync();
            return trafficConditions;
        }

        public async Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition trafficCondition)
        {
            return await _trafficConditionRepository.SaveTrafficConditionAsync(trafficCondition);
        }
    }
}
