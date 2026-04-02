using System.ComponentModel.DataAnnotations;
using CitizenHackathon2025.DTOs.Validation;


namespace CitizenHackathon2025.DTOs.Requests
{
    public sealed class CreateMessageRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 3)]
        [NoProfanity]
        public string Content { get; set; } = "";
    }
}
