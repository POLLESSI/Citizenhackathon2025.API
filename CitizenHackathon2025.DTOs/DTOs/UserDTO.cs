using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class UserDTO
    {
    #nullable disable
        public int Id { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [DisplayName("Email : ")] 
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string Pwd { get; set; }
        [DisplayName("Role : ")]
        public string Role { get; set; }
        public bool Active { get; set; } = true;
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.