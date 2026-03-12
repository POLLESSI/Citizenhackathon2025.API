namespace CitizenHackathon2025.Domain.Entities
{
    public class GptPromptRequest
    {
    #nullable disable
        public string Prompt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
