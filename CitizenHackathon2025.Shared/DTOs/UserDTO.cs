
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class UserDTO
    {
    #nullable disable
        [DisplayName("Email : ")]
        public string Email { get; set; }
        [DisplayName("Password : ")]
        public string Pwd { get; set; }
        [DisplayName("Role : ")]
        public string Role { get; set; }
    }
}
