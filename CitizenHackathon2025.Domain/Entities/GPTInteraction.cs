﻿namespace CitizenHackathon2025.Domain.Entities
{
    public class GPTInteraction
    {
    #nullable disable
        public int Id { get; set; }
        public string Prompt { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; } = true;
    }
}









































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.