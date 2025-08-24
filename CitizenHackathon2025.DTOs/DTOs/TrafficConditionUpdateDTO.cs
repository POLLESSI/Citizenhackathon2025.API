namespace CitizenHackathon2025.DTOs.DTOs
{
    public class TrafficConditionUpdateDTO
    {
    #nullable disable
        public int Id { get; set; } 
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime DateCondition { get; set; }
        public string CongestionLevel { get; set; }
        public string IncidentType { get; set; }
    }
}
