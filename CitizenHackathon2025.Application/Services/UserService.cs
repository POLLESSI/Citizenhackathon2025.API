using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Common;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using Dapper;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IDbConnection _dbConnection;

        public UserService(IUserRepository userRepository, IUserHubService hubService, ILogger<UserService> logger, IDbConnection dbConnection)
        {
            _userRepository = userRepository;
            _hubService = hubService;
            _logger = logger;
            _dbConnection = dbConnection;
        }

        public async Task DeactivateUserAsync(int id)
        {
            try
            {
                await _userRepository.DeactivateUserAsync(id);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error deleting user : {ex.ToString}");
            }
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
        {
            return await _userRepository.GetAllActiveUsersAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            const string sql = @"
            SELECT Email, PasswordHash, SecurityStamp, Role, Status
            FROM [User]
            WHERE Email = @Email;
        ";

            return await _dbConnection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                return _userRepository.GetUserByIdAsync(id);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting user : {ex.ToString}");
            }
            return null;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var user = await _userRepository.LoginUsingProcedureAsync(email, password);
            return user != null;
        }
        public async Task<UserDTO?> LoginUsingProcedureAsync(string email, string password)
        {
            var user = await _userRepository.LoginUsingProcedureAsync(email, password);
            return user != null ? user.Adapt<UserDTO>() : null;
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            const string sql = @"
            INSERT INTO [User] (Email, PasswordHash, SecurityStamp, Role, Status)
            VALUES (@Email, @PasswordHash, @SecurityStamp, @Role, @Status);
        ";

            try
            {
                var parameters = new
                {
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    SecurityStamp = user.SecurityStamp,
                    Role = (int)user.Role,
                    Status = (int)user.Status
                };

                var rowsAffected = await _dbConnection.ExecuteAsync(sql, parameters);
                return rowsAffected == 1;
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint (ex: Email already exists)
            {
                // Logiquement : Violation de clé unique
                return false;
            }
            catch (Exception ex)
            {
                // Log si besoin
                throw new ApplicationException("Erreur lors de l'enregistrement de l'utilisateur.", ex);
            }
        }


        public void SetRole(int id, string? role)
        {
            try
            {
                _userRepository.SetRole(id, role);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error changing rôle: {ex.ToString}");
            }
        }

        public User UpdateUser(User user)
        {
            try
            {
                var updatedUser = _userRepository.UpdateUser(user);
                if (updatedUser != null)
                {
                    _hubService.NotifyUserUpdated(updatedUser);
                }
                return updatedUser;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                Console.WriteLine($"Validation error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user : {ex}");
            }
            return null;
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.