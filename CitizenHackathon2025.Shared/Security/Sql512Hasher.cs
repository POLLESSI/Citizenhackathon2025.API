using System.Security.Cryptography;
using System.Text;

public static class Sql512Hasher
{
    public static byte[] Compute(string? password, Guid securityStamp)
    {
        var combined = (password ?? string.Empty).Trim() + securityStamp.ToString(); // Hyphenated GUID
        var bytes = Encoding.Unicode.GetBytes(combined); // NVARCHAR = UTF-16 LE
        return SHA512.HashData(bytes);
    }
}














































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.