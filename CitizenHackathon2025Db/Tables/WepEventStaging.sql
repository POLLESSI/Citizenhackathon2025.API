CREATE TABLE [dbo].[WepEventStaging]
(
	[Id] BIGINT IDENTITY PRIMARY KEY,
    [ExternalId] NVARCHAR(128) NOT NULL,
    [PlaceExternalId] NVARCHAR(128) NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Latitude] DECIMAL(9,6) NULL,
    [Longitude] DECIMAL(9,6) NULL,
    [DateEvent] DATETIME2(0) NOT NULL,
    [ExpectedCrowd] INT NULL,
    [IsOutdoor] BIT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [SourceUpdatedAtUtc] DATETIME2(3) NULL,
    [ImportedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [Payload] NVARCHAR(MAX) NULL
);
GO

CREATE UNIQUE INDEX UX_WepEventStaging_ExternalId
ON dbo.WepEventStaging(ExternalId);
GO










































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.