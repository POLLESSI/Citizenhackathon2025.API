using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class SuggestionDTO
    {
#nullable disable
        [DisplayName("User ID : ")]
        public int UserId { get; set; }
        [DisplayName("Date of Suggestion : ")]
        public DateTime DateSuggestion { get; set; }
        [DisplayName("Original Place : ")]
        public string OriginalPlace { get; set; }
        [DisplayName("Suggested Alternatives : ")]
        public string SuggestedAlternatives { get; set; }
        [DisplayName("Reason : ")]
        public string Reason { get; set; }
    }
}
