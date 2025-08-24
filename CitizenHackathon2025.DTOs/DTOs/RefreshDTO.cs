using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class RefreshDTO
    {
        // Can be UserId or Email depending on the chosen flow
        [Required(ErrorMessage = "User identifier is required.")]
        [DisplayName("User Identifier")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Refresh token is required.")]
        [DisplayName("Refresh Token")]
        public string RefreshToken { get; set; } = string.Empty;

        // Optional: allows you to know if the request comes from a browser or an external API
        [DisplayName("Client Info")]
        public string? ClientInfo { get; set; }
    }
}
