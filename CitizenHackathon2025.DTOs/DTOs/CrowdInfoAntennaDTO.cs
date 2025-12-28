namespace CitizenHackathon2025.DTOs.DTOs
{
    public class CrowdInfoAntennaDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? Description { get; set; }
    }
}
