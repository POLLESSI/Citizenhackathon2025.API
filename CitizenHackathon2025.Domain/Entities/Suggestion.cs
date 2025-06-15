namespace Citizenhackathon2025.Domain.Entities
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
    }
}

