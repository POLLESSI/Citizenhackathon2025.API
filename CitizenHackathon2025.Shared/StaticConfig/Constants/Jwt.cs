using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Shared.StaticConfig.Constants
{
    public static class Jwt
    {
        public const int ExpirationMinutes = 30;
        public const string SecurityAlgorithm = "HS512";
    }
}
