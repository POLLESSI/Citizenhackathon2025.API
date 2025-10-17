using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class CrowdInfoMappingExtensions
    {
        public static CrowdInfoDTO ToDTO(this CrowdInfo entity) => entity.MapToCrowdInfoDTO();

        public static CrowdInfo ToEntity(this CrowdInfoDTO dto) => dto.MapToCrowdInfo();

        public static CrowdInfoDTO WithTimestamp(this CrowdInfoDTO dto) => dto.MapToCrowdInfoWithTimestamp();
    }
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.