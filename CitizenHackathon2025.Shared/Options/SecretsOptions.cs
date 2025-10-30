namespace CitizenHackathon2025.Shared.Options
{
    public class SecretsOptions 
    { 
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5); 
        public bool CacheEmptyOnNotFound { get; set; } = false; 
    }
}
