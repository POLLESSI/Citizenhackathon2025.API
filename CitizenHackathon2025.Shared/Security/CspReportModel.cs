using System.Text.Json.Serialization;

namespace CitizenHackathon2025.DTOs.Security
{
    public class CspReportModel
    {
    #nullable disable
        [JsonPropertyName("csp-report")]
        public CspReportContent Report { get; set; }
    }
}
