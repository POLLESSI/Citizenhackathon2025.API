CREATE VIEW dbo.vUserSessions
AS
SELECT
    us.Id,
    us.UserEmail,
    us.Jti,
    us.RefreshFamilyId,
    us.IssuedAtUtc,
    us.ExpiresAtUtc,
    us.LastSeenUtc,
    us.Source,
    us.Ip,
    us.UserAgent,
    us.IsRevoked,
    us.IsExpired       
FROM dbo.UserSessions AS us;










































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.