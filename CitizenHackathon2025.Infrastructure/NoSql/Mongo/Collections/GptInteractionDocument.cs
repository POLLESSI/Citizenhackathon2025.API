using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections
{
    public sealed class GptInteractionDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public int? SqlInteractionId { get; set; }

        public string PromptHash { get; set; } = string.Empty;

        public string PromptPreview { get; set; } = string.Empty;

        public string Response { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public bool Success { get; set; }

        public string? Error { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}



























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.