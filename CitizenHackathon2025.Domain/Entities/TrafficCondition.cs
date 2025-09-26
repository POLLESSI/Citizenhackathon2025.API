namespace CitizenHackathon2025.Domain.Entities
{
    public class TrafficCondition
    {
    #nullable disable
        public int Id { get; private set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime DateCondition { get; set; }
        public string CongestionLevel { get; set; }
        public string IncidentType { get; set; }
        public bool Active { get; private set; } = true;

        // 👇 smooth method to reassign the ID during an update
        public TrafficCondition WithId(int id)
        {
            this.Id = id;
            return this;
        }
        // If you want to manipulate the state:
        public void Activate() => this.Active = true;
        public void Deactivate() => this.Active = false;
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.