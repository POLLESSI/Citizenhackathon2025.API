namespace CitizenHackathon2025.Domain.Entities
{
    public class UserMessage
    {
        public int Id { get; set; }
        public string? UserId { get; set; } = null!;
        public string? SourceType { get; set; } = null!;
        public int? SourceId { get; set; } = null!;
        public string? RelatedName { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Tags { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;
    }
}






















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.