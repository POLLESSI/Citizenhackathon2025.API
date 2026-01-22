using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class SuggestionDTO : IValidatableObject
    {
    #nullable disable
        public int Id { get; set; }

        [Required, Range(1, int.MaxValue)]
        [DisplayName("User ID : ")]
        public int UserId { get; set; }

        [Required]
        [DisplayName("Date of Suggestion : ")]
        public DateTime DateSuggestion { get; set; }

        // ✅ You can make these texts optional on the API side.
        // because the “source of truth” becomes PlaceId/EventId.
        [StringLength(128)]
        public string? OriginalPlace { get; set; }

        [StringLength(256)]
        public string? SuggestedAlternatives { get; set; }

        [Required, StringLength(256)]
        public string Reason { get; set; }

        public bool Active { get; set; } = true;

        public string? Message { get; set; }
        public string? Context { get; set; }

        // ✅ Strong references
        public int? EventId { get; set; }
        public int? PlaceId { get; set; }

        // ✅ If you want to continue transporting coordinates to the front: OK, but not mandatory
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationLabel { get; set; }
        public double? DistanceKm { get; set; }
        public string? Title { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EventId is null && PlaceId is null)
            {
                yield return new ValidationResult(
                    "Either EventId or PlaceId must be provided.",
                    new[] { nameof(EventId), nameof(PlaceId) });
            }
        }
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.