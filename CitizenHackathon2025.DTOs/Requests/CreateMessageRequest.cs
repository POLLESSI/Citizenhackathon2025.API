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
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.