CREATE TABLE [dbo].[UserSessions] (
    [Id]              BIGINT IDENTITY(1,1) NOT NULL,
    [UserEmail]       NVARCHAR(64)   NOT NULL,
    [Jti]             CHAR(36)       NOT NULL,
    [RefreshFamilyId] UNIQUEIDENTIFIER NULL,
    [IssuedAtUtc]     DATETIME2(3)   NOT NULL,
    [ExpiresAtUtc]    DATETIME2(3)   NOT NULL,
    [LastSeenUtc]     DATETIME2(3)   NOT NULL,
    [Source]          TINYINT        NOT NULL,
    [Ip]              NVARCHAR(64)   NULL,
    [UserAgent]       NVARCHAR(256)  NULL,
    [IsRevoked]       BIT            NOT NULL DEFAULT 0,
    -- ⚠️ without PERSISTED, because SYSUTCDATETIME() is non-deterministic
    [IsExpired]       AS (CASE WHEN [ExpiresAtUtc] <= SYSUTCDATETIME() THEN 1 ELSE 0 END),
    CONSTRAINT [PK_UserSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_UserSessions_Jti] UNIQUE ([Jti])
);
GO

CREATE INDEX IX_UserSessions_UserEmail_Active
ON dbo.UserSessions(UserEmail)
WHERE IsRevoked = 0;














































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.