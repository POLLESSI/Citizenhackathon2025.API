using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.User;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IUserService
    {
    #nullable disable
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllActiveUsersAsync();
        Task<bool> RegisterUserAsync(string email, string passwordHash, string role);
        Task<bool> LoginAsync(string email, string password);
        Task<bool> LoginAsync(LoginDTO loginDto);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
        User? UpdateUser(User user);
    }
}
