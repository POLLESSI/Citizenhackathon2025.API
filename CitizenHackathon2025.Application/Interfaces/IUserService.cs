using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserService
    {
    #nullable disable
        Task<Users> AuthenticateAsync(string email, string password);
        Task<Users> GetUserByEmailAsync(string email);
        Task<Users> GetUserByIdAsync(int id);
        Task<IEnumerable<Users>> GetAllActiveUsersAsync();
        Task<UserDTO> RegisterUserAsync(string email, string password, UserRole role);
        Task<bool> LoginAsync(string email, string password);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
        Users? UpdateUser(Users user);
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.