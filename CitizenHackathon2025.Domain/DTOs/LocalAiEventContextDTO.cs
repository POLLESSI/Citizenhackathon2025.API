namespace CitizenHackathon2025.Domain.DTOs
{
    public sealed class LocalAiEventContextDTO
    {
        public int Id { get; set; }

        public string? City { get; set; }
        public string? Title { get; set; }

        public DateTime? EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public int? CrowdLevel { get; set; }
        public int? MaxCapacity { get; set; }

        public string? Advice { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.