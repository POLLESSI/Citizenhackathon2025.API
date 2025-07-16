using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions;
using System;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class UserMappingExtensions
    {
        public static UserDTO ToDTO(this User user) => user.UserToDTO();

        public static User ToEntity(this UserDTO dto, Func<string, string, byte[]> hashPasswordFunc, string securityStamp)
            => dto.MapToUserEntity(hashPasswordFunc, securityStamp);
    }
}


















































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.