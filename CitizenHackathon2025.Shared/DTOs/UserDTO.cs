using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class UserDTO
    {
#nullable disable
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [DisplayName("Email : ")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
        [DisplayName("Password : ")]
        public string Pwd { get; set; }
        [DisplayName("Role : ")]
        public string Role { get; set; }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.