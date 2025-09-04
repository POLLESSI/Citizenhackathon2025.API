using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "First email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [DisplayName("Email")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]

        [DisplayName("Password")]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "Role is required.")]
        [EnumDataType(typeof(UserDTO))]
        [DisplayName("Role")]
        /// <summary>"Admin" / "Modo" / "User" / "Guest" ...</summary>
       public string Role { get; set; } = "User";
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.