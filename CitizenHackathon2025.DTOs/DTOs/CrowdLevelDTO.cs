namespace CitizenHackathon2025.DTOs.DTOs
{
#nullable disable
    public class CrowdLevelDTO
   {
       public int Id { get; set; }
       public string PlaceName { get; set; } = "";
       public string Icon { get; set; } = "";
       public string Color { get; set; } = "";
        /// <summary>0..10 or 1..4 depending on your UI usage</summary>
       public CrowdLevelDtoEnum CrowdLevel { get; set; }
       public DateTime Timestamp { get; set; }
       public string Source { get; set; } = "";
       public double Latitude { get; set; }
       public double Longitude { get; set; }
       public bool IsOverloaded { get; set; }
       public string? Description { get; set; }
   }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.