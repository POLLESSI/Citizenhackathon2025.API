using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Domain.Enums;
using Mapster;

namespace CitizenHackathon2025.Application.Mapping
{
    public class UserMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // User → UserDTO
            config.NewConfig<User, UserDTO>()
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.Role, src => src.Role.ToString())
                .Ignore(dest => dest.Pwd); // Pas de hash inverse

            // UserDTO → User (by hand, Pwd → PasswordHash must be treated separately)
            config.NewConfig<UserDTO, User>()
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.Role, src => Enum.Parse<UserRole>(src.Role ?? "", true))
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.PasswordHash)
                .Ignore(dest => dest.Status) // to be defined later
                .Ignore(dest => dest.Active);
        }
    }
}
