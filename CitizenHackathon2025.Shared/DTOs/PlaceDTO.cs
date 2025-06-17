using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class PlaceDTO
    {
#nullable disable
        [Required]
        [DisplayName("Place Name : ")]
        public string Name { get; set; }
        [Required]
        [DisplayName("Place Type : ")]
        public string Type { get; set; }
        [Required]
        [DisplayName("Indoor ? : ")]
        public string Indoor { get; set; }
        [Required]
        [DisplayName("Latitude : ")]
        public string Latitude { get; set; }
        [Required]
        [DisplayName("Longitude : ")]
        public string Longitude { get; set; }
        [Required]
        [DisplayName("Capacity : ")]
        public string Capacity { get; set; }
        [DisplayName("Tags : ")]
        public string Tag { get; set; }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.