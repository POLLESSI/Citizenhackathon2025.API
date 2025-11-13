using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Shared.Extensions
{
    public static class TrafficConditionDTOExtensions
    {
        public static TrafficConditionDTO MapToTrafficConditionDTO(this TrafficCondition entity)
       => new()
       {
           Latitude = entity.Latitude,       // ✅ decimal → decimal
           Longitude = entity.Longitude,     // ✅
           DateCondition = entity.DateCondition,
           CongestionLevel = entity.CongestionLevel,
           IncidentType = entity.IncidentType
       };
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.