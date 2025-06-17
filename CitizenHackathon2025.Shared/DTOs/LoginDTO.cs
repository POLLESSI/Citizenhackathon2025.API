
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(64)]
        [DisplayName("Email")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MaxLength(64)]
        [DisplayName("Password")]
        public string Password { get; set; } = string.Empty;
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.