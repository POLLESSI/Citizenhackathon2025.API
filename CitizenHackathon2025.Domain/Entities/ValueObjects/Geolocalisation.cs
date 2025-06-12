
namespace Citizenhackathon2025.Domain.Entities.ValueObjects
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
