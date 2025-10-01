namespace CitizenHackathon2025.DTOs.DTOs
{
    public class LogsDTO
    {
        #nullable disable
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Exception { get; set; }
        public string SourceContext { get; set; }
        public string RequestPath { get; set; }
        public string RequestId { get; set; }
        public string UserName { get; set; }
        public string Properties { get; set; }
    }
}
