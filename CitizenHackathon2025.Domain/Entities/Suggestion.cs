namespace Citizenhackathon2025.Domain.Entities
{
    public class Suggestion
    {
#nullable disable
        public int Id { get; private set; }
        public int UserId { get; set; }
        public DateTime DateSuggestion { get; set; }
        public string OriginalPlace { get; set; }
        public string SuggestedAlternatives { get; set; }
        public string Reason { get; set; }
        public bool Active { get; private set; } = true;
    }
}

