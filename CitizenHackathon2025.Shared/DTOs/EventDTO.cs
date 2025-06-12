using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class EventDTO
    {
#nullable disable
        [Required]
        [DisplayName("Event Name : ")]
        public string Name { get; set; }
        [Range(-90, 90, ErrorMessage = "Invalid latitude.")]
        [DisplayName("Latitude : ")]
        public string Latitude { get; set; }
        [Range(-180, 180, ErrorMessage = "Invalid longitude.")]
        [DisplayName("Longitude : ")]
        public string Longitude { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DisplayName("Event Date : ")]
        public DateTime DateEvent { get; set; }
        [Range(1, 1000000, ErrorMessage = "Expected number of people invalid.")]
        [DisplayName("Expected Crowd : ")]
        public string? ExpectedCrowd { get; set; }
        [DisplayName("Is Outdoor : ")]
        public string IsOutdoor { get; set; }
    }
}
