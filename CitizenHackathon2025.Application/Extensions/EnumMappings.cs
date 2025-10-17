using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Application.Extensions
{
    public static class EnumMappings
    {
        public static UserStatus ToUserStatus(this Status status)
        {
            return status switch
            {
                Status.Pending => UserStatus.AwaitingConfirmation,
                Status.Active => UserStatus.Active,
                Status.Inactive => UserStatus.Inactive,
                Status.Suspended => UserStatus.Locked,
                Status.Archived => UserStatus.Inactive,
                Status.Deleted => UserStatus.Banned,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }

        public static Status ToStatus(this UserStatus userStatus)
        {
            return userStatus switch
            {
                UserStatus.AwaitingConfirmation => Status.Pending,
                UserStatus.Active => Status.Active,
                UserStatus.Inactive => Status.Inactive,
                UserStatus.Locked => Status.Suspended,
                UserStatus.Banned => Status.Deleted,
                _ => Status.Inactive
            };
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.