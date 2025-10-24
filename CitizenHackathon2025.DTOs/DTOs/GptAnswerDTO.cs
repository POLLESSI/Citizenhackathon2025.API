namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class GptAnswerDTO
    {
        public int? Id { get; set; }
        public string Prompt { get; set; } = "";
        public string Response { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
