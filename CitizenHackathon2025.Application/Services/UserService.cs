using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Common;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Shared.Utils;
using Dapper;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Application.Services
{
    public class UserService : IUserService
    {
#nullable disable
        private readonly IUserRepository _userRepository;
        private readonly CitizenHackathon2025.Application.Interfaces.IUserHubService _hubService;
        private readonly ILogger<UserService> _logger;
        private readonly IDbConnection _connection;

        public UserService(IUserRepository userRepository, IUserHubService hubService, ILogger<UserService> logger, IDbConnection connection)
        {
            _userRepository = userRepository;
            _hubService = hubService;
            _logger = logger;
            _connection = connection;
        }

        public async Task DeactivateUserAsync(int id)
        {
            await _userRepository.DeactivateUserAsync(id);
            await _hubService.NotifyUserDeactivated(id);
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
            => await _userRepository.GetAllActiveUsersAsync();

        public async Task<User> GetUserByEmailAsync(string email)
            => await _userRepository.GetUserByEmailAsync(email);

        public async Task<User> GetUserByIdAsync(int id)
            => await _userRepository.GetUserByIdAsync(id);

        public async Task<bool> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null) return false;

            var hash = HashHelper.HashPassword(password, user.SecurityStamp.ToString());
            return StructuralComparisons.StructuralEqualityComparer.Equals(hash, user.PasswordHash);
        }

        public async Task<UserDTO> RegisterUserAsync(string email, string password, UserRole role)
        {
            var stamp = Guid.NewGuid();
            var passwordHash = HashHelper.HashPassword(password, stamp.ToString());

            var newUser = new User
            {
                Email = email,
                Role = role,
                SecurityStamp = stamp,
                PasswordHash = passwordHash,
                Status = Status.Pending
            };
            newUser.Activate();

            await _userRepository.RegisterUserAsync(email, passwordHash, newUser);
            await _hubService.NotifyUserRegistered(newUser.Email);

            return new UserDTO
            {
                Email = newUser.Email,
                Role = newUser.Role.ToString(),
                Active = newUser.Active
            };
        }

        public void SetRole(int id, string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
                throw new ArgumentException("Invalid role format", nameof(role));

            _userRepository.SetRole(id, parsedRole.ToString());
        }

        public User UpdateUser(User user)
           => _userRepository.UpdateUser(user);
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.