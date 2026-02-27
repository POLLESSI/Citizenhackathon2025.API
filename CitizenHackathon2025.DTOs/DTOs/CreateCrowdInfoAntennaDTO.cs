using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class CreateCrowdInfoAntennaDTO
    {
        [MaxLength(64)]
        public string? Name { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [MaxLength(256)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxCapacity { get; set; }
    }
}
