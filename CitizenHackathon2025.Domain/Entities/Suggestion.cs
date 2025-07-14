namespace CitizenHackathon2025.Domain.Entities
{
    public class Suggestion
    {
    #nullable disable
        public int Id { get; set; }
        public int User_Id { get; set; }
        public DateTime DateSuggestion { get; set; }
        public string? OriginalPlace { get; set; }
        public string? SuggestedAlternatives { get; set; }
        public string? Reason { get; set; }
        public bool Active { get; set; }
        public DateTime? DateDeleted { get; set; }

        // Links to external entities
        public int? EventId { get; set; }
        public int? ForecastId { get; set; }
        public int? TrafficId { get; set; }
        public string? LocationName { get; set; }
    }
}






































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.