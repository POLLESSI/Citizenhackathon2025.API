using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Extensions
{
    public static class UserPublicMappingExtensions
    {
        public static UserPublicDTO ToPublicDTO(
            this Users user)
        {
            ArgumentNullException.ThrowIfNull(user);

            return new UserPublicDTO
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Active = user.Active
            };
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.