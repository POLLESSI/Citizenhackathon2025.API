
using Citizenhackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Shared.DTOs
{
    public class RegisterDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public Role? Role { get; set; } // ✅ nullable enum
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.