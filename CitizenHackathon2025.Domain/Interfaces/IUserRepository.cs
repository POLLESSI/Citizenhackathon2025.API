using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.User;


namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllActiveUsersAsync();
        Task<bool> RegisterUserAsync(string email, byte[] passwordHash, User user);
        Task<bool> LoginAsync(string email, string password);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
    }
}
