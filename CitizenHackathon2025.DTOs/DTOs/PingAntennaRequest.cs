using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class PingAntennaRequest
    {
        [Range(1, int.MaxValue)]
        public int AntennaId { get; set; }

        public int? EventId { get; set; }

        [Required]
        public string DeviceHashBase64 { get; set; } = default!;

        public string? IpHashBase64 { get; set; }

        public string? MacHashBase64 { get; set; }

        [Range(0, 255)]
        public byte Source { get; set; }

        [Range(-150, 0)]
        public short? SignalStrength { get; set; }

        [MaxLength(16)]
        public string? Band { get; set; }

        public string? AdditionalJson { get; set; }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.