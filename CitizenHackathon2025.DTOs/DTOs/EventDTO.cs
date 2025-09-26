using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class EventDTO
    {
    #nullable disable
        public int Id { get; set; }
        [Required]
        [DisplayName("Event Name : ")]
        public string Name { get; set; }
        [Required]
        [Range(-90, 90, ErrorMessage = "Invalid latitude.")]
        [DisplayName("Latitude : ")]
        public double Latitude { get; set; }
        [Required]
        [Range(-180, 180, ErrorMessage = "Invalid longitude.")]
        [DisplayName("Longitude : ")]
        public double Longitude { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DisplayName("Event Date : ")]
        public DateTime DateEvent { get; set; }
        [Range(1, 1000000, ErrorMessage = "Expected number of people invalid.")]
        [DisplayName("Expected Crowd : ")]
        public int? ExpectedCrowd { get; set; }
        [Required]
        [DisplayName("Is Outdoor : ")]
        public bool IsOutdoor { get; set; }
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.