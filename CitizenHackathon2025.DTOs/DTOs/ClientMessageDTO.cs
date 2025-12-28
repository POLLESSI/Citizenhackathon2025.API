namespace CitizenHackathon2025.DTOs.DTOs
{
    public class ClientMessageDTO
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? SourceType { get; set; }
        public int? SourceId { get; set; }
        public string? RelatedName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Tags { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.