using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.ODWB.Models
{
    public sealed class OdwbRecordsResponse<T>
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("results")]
        public List<T> Results { get; set; } = new();
    }
}
