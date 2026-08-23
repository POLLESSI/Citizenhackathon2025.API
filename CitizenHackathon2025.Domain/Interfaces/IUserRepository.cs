using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<Users?> GetUserByEmailAsync(string email);
        Task<Users?> GetUserByIdAsync(int id);
        Task<IEnumerable<Users>> GetAllActiveUsersAsync();
        Task<Users> RegisterUserAsync(Users user);
        Task DeactivateUserAsync(int id);
        Task AnonymizeUserAsync(int userId, CancellationToken ct = default);
        void SetRole(int id, string? role);
        Users? UpdateUser(Users user);

        // =====================================================
        // PASSWORD HASH V2
        // =====================================================
        Task UpdatePasswordHashV2Async(int userId, string passwordHashV2);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.