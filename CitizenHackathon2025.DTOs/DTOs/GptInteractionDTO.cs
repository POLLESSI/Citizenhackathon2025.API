namespace CitizenHackathon2025.DTOs.DTOs
{
    public class GptInteractionDTO
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;

        // Hash stored server-side (useful on admin/diagnostic side, optional on client UI side)
        public string PromptHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.