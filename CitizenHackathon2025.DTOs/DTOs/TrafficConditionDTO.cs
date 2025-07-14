using System.ComponentModel;

namespace CitizenHackathon2025.DTOs.DTOs
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

        public string Location { get; set; }
        public string Level { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}









































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.