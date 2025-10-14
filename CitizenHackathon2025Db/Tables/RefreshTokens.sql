CREATE TABLE [dbo].[RefreshTokens]
(
    [Id]           INT IDENTITY PRIMARY KEY,
    [Token]        NVARCHAR(128) NOT NULL UNIQUE,   -- (optional: to be deleted after migration)
    [Email]        NVARCHAR(256) NOT NULL,
    [ExpiryDate]   DATETIME2      NOT NULL,
    [IsRevoked]    BIT            NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    [Status]       INT            NOT NULL DEFAULT 1,   -- 1=Active, 2=Revoked, 3=Expired
    [TokenHash]    VARBINARY(32)  NULL,                 -- ✅ added
    [TokenSalt]    VARBINARY(16)  NULL                  -- ✅ added
);
GO

-- if you need to find it quickly by email/CreatedAt (the normal case)
CREATE INDEX IX_RefreshTokens_Email_Active
ON dbo.RefreshTokens(Email, Status, ExpiryDate DESC, CreatedAt DESC);
GO

CREATE TRIGGER [dbo].[OnDeleteRefreshTokens]
ON [dbo].[RefreshTokens]
INSTEAD OF DELETE
AS
BEGIN
    UPDATE [dbo].[RefreshTokens]
    SET Status = 2, IsRevoked = 1
    WHERE Id IN (SELECT Id FROM deleted);
END;
GO

-- =============================================
-- Notes:
-- - Status codes:
--     1 = Active
--     2 = Revoked
--     3 = Expired
-- - All token logic in C# ignores IsRevoked and uses Status + ExpiryDate.
-- - IsRevoked is only updated for historical/legacy purposes.
-- =============================================

















































































    --// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.