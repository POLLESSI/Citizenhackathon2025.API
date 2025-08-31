namespace CitizenHackathon2025.Shared.Utils
{
    public static class HashHelper
    {
        public static byte[] HashPassword(string pwd, string securityStamp)
        {
            using var sha512 = System.Security.Cryptography.SHA512.Create();
            var combined = System.Text.Encoding.UTF8.GetBytes(pwd + securityStamp);
            return sha512.ComputeHash(combined);
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.