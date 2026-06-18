CREATE TABLE [dbo].[DisasterAlert]
(
	[Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
	[DisasterType] TINYINT NOT NULL,
    [Severity] TINYINT NOT NULL,
    [Latitude] DECIMAL(9,6) NOT NULL,
    [Longitude]  DECIMAL(9,6) NOT NULL,
    [PlaceName] NVARCHAR(128) NULL,
    [Description] NVARCHAR(512) NULL,
    [ConfirmationCount] INT NOT NULL,
    [RequiredCount] INT NOT NULL,
    [Status] NVARCHAR(32) NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [ExpiresAtUtc] DATETIME2(3) NULL,
    [Active] BIT NOT NULL DEFAULT 1
)
