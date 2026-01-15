namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class PingAntennaRequest
    {
        public int AntennaId { get; set; }

        // In APIs, the hash is often transmitted in base64.
        public string DeviceHashBase64 { get; set; } = "";

        public string? IpHashBase64 { get; set; }
        public string? MacHashBase64 { get; set; }

        public byte Source { get; set; } = 0;
        public short? SignalStrength { get; set; }
        public string? Band { get; set; }
        public string? AdditionalJson { get; set; }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.