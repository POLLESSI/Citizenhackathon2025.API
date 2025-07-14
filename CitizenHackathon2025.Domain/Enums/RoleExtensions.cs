namespace CitizenHackathon2025.Domain.Enums
{
    public static class RoleExtensions
    {
        public static string ToRoleString(this UserRole role)
        {
            return role.ToString();
        }

        public static UserRole ParseRole(string roleString)
        {
            return Enum.TryParse<UserRole>(roleString, out var role) ? role : throw new ArgumentException("Invalid role");
        }
    }
}
