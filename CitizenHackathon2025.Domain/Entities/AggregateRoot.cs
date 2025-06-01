
namespace Citizenhackathon2025.Domain.Entities
{
    public abstract class AggregateRoot<TId>
    {
    #nullable disable
        public TId Id { get; set; }
    }

}
