namespace CitizenHackathon2025.Domain.ReadRows
{
    public sealed class SuggestionReadRow
    {
        public int Id { get; set; }
        public int User_Id { get; set; }
        public DateTime DateSuggestion { get; set; }
        public string? OriginalPlace { get; set; }
        public string? SuggestedAlternatives { get; set; }
        public string? Reason { get; set; }
        public bool Active { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? EventId { get; set; }
        public int? PlaceId { get; set; }
        public int? ForecastId { get; set; }
        public int? TrafficId { get; set; }
        public string? LocationName { get; set; }

        // enrichment
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationLabel { get; set; }
    }

}
