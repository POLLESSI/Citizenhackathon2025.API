namespace CitizenHackathon2025.Domain.Entities
{
    public class OllamaResponse
    {
    #nullable disable
        public Message Content { get; set; }
        public bool Success { get; set; }

        public class Message
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }
    }

}
