using System.Text.Json.Serialization;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class GeoLocationDTO
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }
}























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.