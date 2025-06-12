using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Common;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public UserService(IUserRepository userRepository, IUserHubService hubService, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _hubService = hubService;
            _logger = logger;
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
            try
            {
                return await _userRepository.GetUserByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email.");
                return null;
            }
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
            try
            {
                return await _userRepository.LoginAsync(email, password);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error loging : {ex.ToString}");
            }
            return false;
        }
        public async Task<bool> LoginAsync(LoginDTO loginDto)
        {
            return await LoginAsync(loginDto.Email, loginDto.Password);
        }
        public async Task<bool> RegisterUserAsync(string email, string password, Role role)
        {
            if (!Validators.IsValidEmail(email))
                throw new ValidationException("Invalid email address.");
            try
            {
                // ✅ Hash the password
                var hashedPassword = Hasher.ComputeHash(password);

                var user = new User
                {
                    Email = email,
                    Role = role
                };

                // ✅ Transmit the hash
                return await _userRepository.RegisterUserAsync(email, hashedPassword, user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering new user : {ex}");
                return false;
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
                var UpdateUser = _userRepository.UpdateUser(user);
                if (UpdateUser != null)
                {
                    throw new KeyNotFoundException("User not found for update.");
                }
                return UpdateUser;
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
