using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class RegisterDTO
    {
        [Required]
        [EmailAddress]
        [MaxLength(64)]
        public string Email { get; set; } =
            string.Empty;

        [Required]
        [MinLength(12)]
        [MaxLength(128)]
        public string Password { get; set; } =
            string.Empty;
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.