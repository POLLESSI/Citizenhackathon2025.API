namespace CitizenHackathon2025.DTOs.DTOs
{
    public class UpdateUserDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public int Role { get; set; }       
        public int Status { get; set; }
        public bool Active { get; set; }
    }
}
