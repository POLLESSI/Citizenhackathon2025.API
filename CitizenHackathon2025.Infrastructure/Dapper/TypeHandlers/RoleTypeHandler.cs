using System.Data;
using CitizenHackathon2025.Domain.Enums;
using Dapper;

namespace CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers
{
    public class RoleTypeHandler : SqlMapper.TypeHandler<UserRole>
    {
        public override void SetValue(IDbDataParameter parameter, UserRole value)
        {
            parameter.Value = (int)value; // stored in int
        }

        public override UserRole Parse(object value)
        {
            if (value == null || value == DBNull.Value)
                return UserRole.User; // fallback

            return Enum.IsDefined(typeof(UserRole), (int)value)
                ? (UserRole)(int)value
                : UserRole.User; // fallback
        }
    }
}











































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.