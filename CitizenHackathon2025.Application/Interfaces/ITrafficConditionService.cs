﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.TrafficCondition;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ITrafficConditionService
    {
#nullable disable
        Task<IEnumerable<TrafficCondition?>> GetLatestTrafficConditionAsync();
        Task<TrafficCondition> SaveTrafficConditionAsync(TrafficCondition @trafficCondition);
        TrafficCondition? UpdateTrafficCondition(TrafficCondition @trafficCondition);
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.