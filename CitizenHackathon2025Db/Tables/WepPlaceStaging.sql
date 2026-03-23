CREATE TABLE [dbo].[WepPlaceStaging]
(
	[Id] BIGINT IDENTITY PRIMARY KEY,
    [ExternalId] NVARCHAR(128) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Type] NVARCHAR(64) NULL,
    [Indoor] BIT NULL,
    [Latitude] DECIMAL(9,6) NULL,
    [Longitude] DECIMAL(9,6) NULL,
    [Capacity] INT NULL,
    [Tag] NVARCHAR(64) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [SourceUpdatedAtUtc] DATETIME2(3) NULL,
    [ImportedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [Payload] NVARCHAR(MAX) NULL
);
GO

CREATE UNIQUE INDEX UX_WepPlaceStaging_ExternalId
ON dbo.WepPlaceStaging(ExternalId);
GO























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.