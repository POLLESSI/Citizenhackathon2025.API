using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class SuggestionDTO
    {
#nullable disable
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
    }
}
