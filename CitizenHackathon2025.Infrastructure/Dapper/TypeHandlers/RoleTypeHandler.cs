using System.Data;
using CitizenHackathon2025.Domain.Enums;
using Dapper;

namespace CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers
{
    public class RoleTypeHandler : SqlMapper.TypeHandler<UserRole>
    {
        public override void SetValue(IDbDataParameter parameter, UserRole value)
        {
            parameter.Value = value.ToString();
        }

        public override UserRole Parse(object value)
        {
            return Enum.TryParse(typeof(UserRole), value.ToString(), out var result)
                ? (UserRole)result
                : UserRole.User; 
        }
    }
}
