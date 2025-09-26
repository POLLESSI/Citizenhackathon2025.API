using System.Text;

namespace CitizenHackathon2025.Shared.Utils
{
    public static class PromptHashing
    {
        public static string ComputeDeterministicHash(string? prompt, string secret)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;

            // 1) Canonization
            var canon = NormalizePrompt(prompt);

            // 2) HMAC-SHA256 (pepper)
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canon));

            // 3) Hex in 64 (NVARCHAR(64))
            return ConvertToHex(bytes);
        }

        private static string NormalizePrompt(string s)
        {
            var t = s.Trim().Normalize(NormalizationForm.FormKC);
            // Option: collapse multiple spaces
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ");
            // Option: deterministic case
            t = t.ToLowerInvariant();
            return t;
        }

        private static string ConvertToHex(byte[] bytes)
        {
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
