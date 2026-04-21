namespace CitizenHackathon2025.Domain.Models
{
    public sealed class AskGptRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
