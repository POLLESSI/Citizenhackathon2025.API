using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<Users?> GetUserByEmailAsync(string email);
        Task<Users?> GetUserByIdAsync(int id);
        Task<IEnumerable<Users>> GetAllActiveUsersAsync();
        Task<Users> RegisterUserAsync(string email, byte[] passwordHash, Users user);
        Task<bool> LoginAsync(string email, string password);
        Task<Users?> LoginUsingProcedureAsync(string email, string password);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
        Users? UpdateUser(Users user);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.