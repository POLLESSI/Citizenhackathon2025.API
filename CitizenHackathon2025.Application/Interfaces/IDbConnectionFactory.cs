using System.Data;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}
