namespace CitizenHackathon2025.Shared.Interfaces
{
    public interface IDeviceHasher
    {
        byte[] ComputeHash(string rawIdentifier);
        string ComputeHashBase64(string rawIdentifier); // optionally
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.