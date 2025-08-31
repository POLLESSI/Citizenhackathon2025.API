CREATE TABLE [dbo].[RefreshTokens]
(
    [Id] INT IDENTITY PRIMARY KEY,
    [Token] NVARCHAR(128) NOT NULL UNIQUE,
    [Email] NVARCHAR(256) NOT NULL,
    [ExpiryDate] DATETIME2 NOT NULL,
    [IsRevoked] BIT NOT NULL DEFAULT 0,  -- kept for historical compatibility
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [Status] INT NOT NULL DEFAULT 1      -- 1=Active, 2=Revoked, 3=Expired
);
GO

-- Index to quickly retrieve tokens by email
CREATE INDEX IX_RefreshTokens_Email ON [dbo].[RefreshTokens](Email);
GO

-- =============================================
-- Optional trigger: prevent hard deletes (soft delete approach)
-- =============================================
-- This ensures historical data is preserved.
-- Instead of deleting, mark token as revoked.
CREATE TRIGGER [dbo].[OnDeleteRefreshTokens]
ON [dbo].[RefreshTokens]
INSTEAD OF DELETE
AS
BEGIN
    UPDATE [dbo].[RefreshTokens]
    SET Status = 2,        -- Revoked
        IsRevoked = 1
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