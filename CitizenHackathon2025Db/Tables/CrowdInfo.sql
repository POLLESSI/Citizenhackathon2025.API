CREATE TABLE [dbo].[CrowdInfo]
(
	[Id] INT IDENTITY,
	[LocationName] NVARCHAR(64) NOT NULL,
	[Latitude] DECIMAL(9, 6) NOT NULL,
	[Longitude] DECIMAL(9, 6) NOT NULL,
	[CrowdLevel] INT NOT NULL,
	[Timestamp] DATETIME2(0) NOT NULL,
	[Active] BIT DEFAULT 1,

	CONSTRAINT [PK_CrowdInfo] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_CrowdInfo_LocationName_Timestamp] UNIQUE (LocationName, [Timestamp])
)

GO

CREATE TRIGGER [dbo].[OnDeleteCrowdInfo]
	ON [dbo].[CrowdInfo]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE CrowdInfo SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END

GO

CREATE INDEX IX_CrowdInfo_Active_Timestamp
ON dbo.CrowdInfo (Active, [Timestamp] DESC);
GO




















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.