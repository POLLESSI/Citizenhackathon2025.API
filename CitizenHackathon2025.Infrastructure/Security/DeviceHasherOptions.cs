namespace CitizenHackathon2025.Infrastructure.Security
{
    public class DeviceHasherOptions
    {
        public string PepperBase64 { get; set; } = ""; // loaded from config/KeyVault
    }
}
