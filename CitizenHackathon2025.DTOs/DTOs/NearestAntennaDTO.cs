namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class NearestAntennaDTO
    {
        public CrowdInfoAntennaDTO Antenna { get; set; } = new();
        public double DistanceMeters { get; set; }
    }
}