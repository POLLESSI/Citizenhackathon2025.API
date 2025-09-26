using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class PlaceDTO
    {
#nullable disable
        [Required]
        [DisplayName("Place Name : ")]
        public string Name { get; set; } = "";
        [Required]
        [DisplayName("Place Type : ")]
        public string Type { get; set; } = "";
        [Required]
        [DisplayName("Indoor ? : ")]
        public bool Indoor { get; set; }
        [Required]
        [DisplayName("Latitude : ")]
        public decimal Latitude { get; set; }
        [Required]
        [DisplayName("Longitude : ")]
        public decimal Longitude { get; set; }
        [Required]
        [DisplayName("Capacity : ")]
        public int Capacity { get; set; }
        [DisplayName("Tags : ")]
        public string Tag { get; set; } = "";
        public bool Active { get; set; } = true;
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.