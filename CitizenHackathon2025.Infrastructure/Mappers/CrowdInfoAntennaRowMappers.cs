using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Infrastructure.Repositories; 

namespace CitizenHackathon2025.Infrastructure.Mappers
{
    internal static class CrowdInfoAntennaRowMappers
    {
        internal static CrowdInfoAntenna ToEntity(this CrowdInfoAntennaNearestRow row)
        {
            return new CrowdInfoAntenna
            {
                Id = row.Id,
                Name = row.Name ?? string.Empty,
                Latitude = (double)row.Latitude,
                Longitude = (double)row.Longitude,
                Active = row.Active,
                CreatedUtc = row.CreatedUtc,
                Description = row.Description,
                MaxCapacity = row.MaxCapacity
            };
        }
    }
}



































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.