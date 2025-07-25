﻿using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IUserService
    {
    #nullable disable
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllActiveUsersAsync();
        Task<UserDTO> RegisterUserAsync(string email, string password, UserRole role);
        Task<bool> LoginAsync(string email, string password);
        Task DeactivateUserAsync(int id);
        void SetRole(int id, string? role);
        User? UpdateUser(User user);
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.