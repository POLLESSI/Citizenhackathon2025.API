using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class TrafficConditionDTOExtensions
    {
        public static TrafficConditionDTO MapToTrafficConditionDTO(this TrafficCondition entity)
        {
            return new TrafficConditionDTO
            {
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DateCondition = entity.DateCondition,
                CongestionLevel = entity.CongestionLevel,
                IncidentType = entity.IncidentType
            };
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.