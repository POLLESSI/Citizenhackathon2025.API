using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class TrafficConditionMappingExtensions
    {
        public static TrafficConditionDTO ToDTO(this TrafficCondition entity) => entity.MapToTrafficConditionDTO();
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.