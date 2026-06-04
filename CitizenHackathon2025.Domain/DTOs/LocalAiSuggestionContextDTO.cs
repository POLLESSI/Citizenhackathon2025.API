namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiSuggestionContextDTO
    {
        public int Id { get; set; }

        public string? OriginalPlace { get; set; }
        public string? SuggestedAlternatives { get; set; }
        public string? Reason { get; set; }
        public string? Message { get; set; }
        public string? Context { get; set; }

        public int? EventId { get; set; }
        public int? PlaceId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; }

        public string? LocationLabel { get; set; }
        public string? Title { get; set; }

        public bool Active { get; set; }
    }
}



























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.