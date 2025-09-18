using System.Text.Json;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Shared.Json
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }
}
