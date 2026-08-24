using CitizenHackathon2025.Domain.Entities;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Security
{
    public sealed class OutZenPasswordHasher : IPasswordHasher<Users>
    {
        private const int ArgonVersion = 19;

        /*
         * OWASP baseline:
         * m = 19456 KiB = 19 MiB
         * t = 2
         * p = 1
         */
        private const int MemorySizeKiB = 19 * 1024;
        private const int Iterations = 2;
        private const int Parallelism = 1;

        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;

        private readonly PasswordHasher<Users>_identityHasher;

        public OutZenPasswordHasher(PasswordHasher<Users> identityHasher)
        {
            _identityHasher = identityHasher ?? throw new ArgumentNullException(nameof(identityHasher));
        }

        public string HashPassword(Users user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (password is null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            /*
             * IMPORTANT:
             * Never Trim() a password.
             */
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            /*
             * Konscious supports password input
             * up to 1024 bytes.
             */
            if (passwordBytes.Length > 1024)
            {
                throw new ArgumentException("Password is too long.", nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);

            var hash = ComputeArgon2id(passwordBytes, salt, MemorySizeKiB, Iterations, Parallelism, HashSizeBytes);

            return BuildEncodedHash(salt, hash, MemorySizeKiB, Iterations, Parallelism);
        }

        public PasswordVerificationResult VerifyHashedPassword(Users user, string hashedPassword, string providedPassword)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(hashedPassword) || providedPassword is null)
            {
                return PasswordVerificationResult.Failed;
            }

            /*
             * ==================================================
             * CURRENT FORMAT: ARGON2ID
             * ==================================================
             */

            if (hashedPassword.StartsWith("$argon2id$", StringComparison.Ordinal))
            {
                return VerifyArgon2id(hashedPassword, providedPassword);
            }

            /*
             * ==================================================
             * FALLBACK: ASP.NET CORE IDENTITY / PBKDF2
             * ==================================================
             *
             * Existing AQAAAA... hashes arrive here.
             */

            PasswordVerificationResult identityResult;

            try
            {
                identityResult = _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }

            if (identityResult == PasswordVerificationResult.Failed)
            {
                return PasswordVerificationResult.Failed;
            }

            /*
             * Password was valid using the old Identity
             * PBKDF2 hash.
             *
             * Ask UserService to re-hash using Argon2id.
             */
            return PasswordVerificationResult.SuccessRehashNeeded;
        }

        private static PasswordVerificationResult VerifyArgon2id(string encodedHash, string password)
        {
            try
            {
                if (!TryParseEncodedHash(encodedHash, out var parsed))
                {
                    return PasswordVerificationResult.Failed;
                }

                var passwordBytes = Encoding.UTF8.GetBytes(password);

                if (passwordBytes.Length > 1024)
                {
                    return PasswordVerificationResult.Failed;
                }

                var actualHash = ComputeArgon2id(passwordBytes, parsed.Salt, parsed.MemorySizeKiB, parsed.Iterations, parsed.Parallelism, parsed.Hash.Length);

                var valid = CryptographicOperations.FixedTimeEquals(actualHash, parsed.Hash);

                if (!valid)
                {
                    return PasswordVerificationResult.Failed;
                }

                /*
                 * If our policy becomes stronger later,
                 * automatically upgrade on successful login.
                 *
                 * We only upgrade weaker hashes.
                 * We do not downgrade stronger parameters.
                 */
                var needsRehash = parsed.Version != ArgonVersion ||
                    parsed.MemorySizeKiB < MemorySizeKiB ||
                    parsed.Iterations < Iterations ||
                    parsed.Parallelism < Parallelism ||
                    parsed.Hash.Length < HashSizeBytes;

                return needsRehash ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
            }
            catch
            {
                /*
                 * Malformed database value must never
                 * crash the authentication endpoint.
                 */
                return PasswordVerificationResult.Failed;
            }
        }

        private static byte[] ComputeArgon2id(byte[] password, byte[] salt, int memorySizeKiB, int iterations, int parallelism, int hashLength)
        {
            using var argon2 =
                new Argon2id(password)
                {
                    Salt = salt,
                    MemorySize = memorySizeKiB,
                    Iterations = iterations,
                    DegreeOfParallelism = parallelism
                };

            return argon2.GetBytes(hashLength);
        }

        private static string BuildEncodedHash(byte[] salt, byte[] hash, int memorySizeKiB, int iterations, int parallelism)
        {
            /*
             * PHC-style Argon2 string:
             *
             * $argon2id$v=19$m=19456,t=2,p=1$salt$hash
             */

            return $"$argon2id$v={ArgonVersion}" + $"$m={memorySizeKiB}," + $"t={iterations}," + $"p={parallelism}" + $"${ToBase64NoPadding(salt)}" + $"${ToBase64NoPadding(hash)}";
        }

        private static bool TryParseEncodedHash(string encodedHash, out ParsedArgonHash parsed)
        {
            parsed = default;

            var parts = encodedHash.Split('$', StringSplitOptions.None);

            /*
             * Expected:
             *
             * [0] ""
             * [1] argon2id
             * [2] v=19
             * [3] m=19456,t=2,p=1
             * [4] salt
             * [5] hash
             */
            if (parts.Length != 6 || parts[1] != "argon2id")
            {
                return false;
            }

            if (!TryParseInt(parts[2], "v=", out var version))
            {
                return false;
            }

            var parameters = parts[3].Split(',');

            if (parameters.Length != 3)
            {
                return false;
            }

            if (!TryParseInt(parameters[0], "m=", out var memory))
            {
                return false;
            }

            if (!TryParseInt(parameters[1], "t=", out var iterations))
            {
                return false;
            }

            if (!TryParseInt(parameters[2], "p=", out var parallelism))
            {
                return false;
            }

            /*
             * Defensive bounds.
             */
            if (memory < 8 * 1024 || memory > 1024 * 1024 || iterations < 1 || iterations > 20 || parallelism < 1 || parallelism > 16)
            {
                return false;
            }

            var salt = FromBase64NoPadding(parts[4]);

            var hash = FromBase64NoPadding(parts[5]);

            if (salt.Length < 16 || hash.Length < 16)
            {
                return false;
            }

            parsed =
                new ParsedArgonHash(version, memory, iterations, parallelism, salt, hash);

            return true;
        }

        private static bool TryParseInt(string value, string prefix, out int result)
        {
            result = 0;

            if (!value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        private static string ToBase64NoPadding(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=');
        }

        private static byte[] FromBase64NoPadding(string value)
        {
            var padding = (4 - value.Length % 4) % 4;

            value += new string('=', padding);

            return Convert.FromBase64String(value);
        }

        private readonly record struct ParsedArgonHash(int Version, int MemorySizeKiB, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.