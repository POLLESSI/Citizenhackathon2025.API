namespace CitizenHackathon2025.Infrastructure.NoSql.Mongo
{
    public sealed class MongoOptions
    {
        public string? ConnectionString { get; set; }
        public string? ConnectionStringSecretName { get; set; }
        public string DatabaseName { get; set; } = "CitizenHackathon2025";
    }
}
