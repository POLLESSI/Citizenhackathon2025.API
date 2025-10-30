namespace CitizenHackathon2025.Shared.Interfaces
{
    public interface IDeviceHasher
    {
        byte[] ComputeHash(string rawIdentifier);
        string ComputeHashBase64(string rawIdentifier); // optionally
    }
}
