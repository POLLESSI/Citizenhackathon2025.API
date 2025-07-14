namespace CitizenHackathon2025.Domain.Entities.ValueObjects
{
    public class Geolocalisation
    {
        public Location Value { get; }

        public Geolocalisation(Location value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
        
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.