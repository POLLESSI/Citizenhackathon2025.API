CREATE TABLE [dbo].[RefreshTokens]
(
	[Id] INT IDENTITY,
    [Token] NVARCHAR(128) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [ExpiryDate] DATETIME2 NOT NULL,
    [IsRevoked] BIT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [Status] INT NOT NULL DEFAULT 1 -- 1=Active, 2=Revoked, 3=Expired

    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_RefreshTokens_Token] UNIQUE ([Token])
)

GO

--CREATE TRIGGER [dbo].[OnDeleteRefreshTokens]
--    ON [dbo].[RefreshTokens]
--    INSTEAD OF DELETE
--    AS
--    BEGIN
--        UPDATE RefreshTokens SET Active = 0
--        WHERE Id IN (SELECT Id FROM deleted)
--    END

















































































    --// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.