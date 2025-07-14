using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface ITrafficConditionRepository
    {
        Task<IEnumerable<TrafficCondition?>> GetLatestTrafficConditionAsync();
        Task<TrafficCondition?> GetByIdAsync(int id);
        Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition @trafficCondition);
        TrafficCondition? UpdateTrafficCondition(TrafficCondition @trafficCondition);
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.