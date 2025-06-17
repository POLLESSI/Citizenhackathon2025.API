
using System.ComponentModel;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class TrafficConditionDTO
    {
#nullable disable
        [DisplayName("Latitude : ")]
        public string Latitude { get; set; }
        [DisplayName("Longitude : ")]
        public string Longitude { get; set; }
        [DisplayName("Traffic Condition Date : ")]
        public DateTime DateCondition { get; set; }
        [DisplayName("Congestion Level : ")]
        public string CongestionLevel { get; set; }
        [DisplayName("Incident Type : ")]
        public string IncidentType { get; set; }
    }
}










































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.