using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllActiveUsersAsync();
        Task<User> RegisterUserAsync(string email, byte[] passwordHash, User user);
        Task<bool> LoginAsync(string email, string password);
        Task<User?> LoginUsingProcedureAsync(string email, string password);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
        User? UpdateUser(User user);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.