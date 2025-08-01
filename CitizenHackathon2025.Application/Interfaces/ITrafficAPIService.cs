﻿using CitizenHackathon2025.DTOs.DTOs;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface ITrafficApiService
    {
        /// <summary>
        /// Retrieves traffic conditions from the Waze API (Connected Citizens).
        /// </summary>
        Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude);
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.