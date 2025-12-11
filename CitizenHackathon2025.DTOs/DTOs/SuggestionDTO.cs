using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class SuggestionDTO
    {
#nullable disable
        public int Id { get; set; }
        [Required(ErrorMessage = "User ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be greater than 0.")]
        [DisplayName("User ID : ")]
        public int UserId { get; set; }
        [Required(ErrorMessage = "The date of the suggestion is mandatory.")]
        [DataType(DataType.Date)]
        [DisplayName("Date of Suggestion : ")]
        public DateTime DateSuggestion { get; set; }
        [Required(ErrorMessage = "Original location is required.")]
        [StringLength(64, ErrorMessage = "The original location cannot exceed 64 characters.")]
        [DisplayName("Original Place : ")]
        public string OriginalPlace { get; set; }
        [Required(ErrorMessage = "Alternatives are required.")]
        [StringLength(256, ErrorMessage = "The alternative suggestion cannot exceed 256 characters.")]
        [DisplayName("Suggested Alternatives : ")]
        public string SuggestedAlternatives { get; set; }
        [Required(ErrorMessage = "The reason for the suggestion is required.")]
        [StringLength(256, ErrorMessage = "The reason cannot exceed 256 characters.")]
        [DisplayName("Reason : ")]
        //[NoProfanity]
        public string Reason { get; set; }
        public bool Active { get; set; } = true;
        public string Message { get; set; }
        public string Context { get; set; }
        public int? EventId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
        public string? Title { get; set; }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.