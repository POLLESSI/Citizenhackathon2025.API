namespace CitizenHackathon2025.Domain.Entities
{
    public class MistralResponse
    {
    #nullable disable
        public Choice[] Choices { get; set; }
        public class Choice
        {
            public Message Message { get; set; }
        }
        public class Message
        {
            public string Content { get; set; }
        }
    }
}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.