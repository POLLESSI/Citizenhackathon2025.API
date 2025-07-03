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