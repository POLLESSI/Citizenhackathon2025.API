CREATE TABLE [dbo].[UserMessage]
(
	[Id] INT IDENTITY PRIMARY KEY,
	[UserId] NVARCHAR(64) NULL,         -- email or userId (optional)
	[SourceType] NVARCHAR(64) NULL,     -- 'Crowd','Event','Place','Traffic','Weather','Other'
	[SourceId] INT NULL,                -- Id of CrowdInfo / Event / Place / TrafficCondition
	[RelatedName] NVARCHAR(64) NULL,    -- LocationName / Event.Name / Place.Name
	[Latitude] DECIMAL(9,6) NULL,
	[Longitude] DECIMAL(9,6) NULL,
	[Tags] NVARCHAR(256) NULL,          -- extracted keywords (or GPT)
	[Content] NVARCHAR(1024) NOT NULL,
	[CreatedAt] DATETIME2(7) DEFAULT (SYSUTCDATETIME()), 
	[Active] BIT DEFAULT 1
 
);

GO

CREATE TRIGGER [dbo].[OnDeleteUserMessage]
ON [dbo].[UserMessage]
INSTEAD OF DELETE
AS
BEGIN
	UPDATE UserMessage SET Active = 0
	WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE INDEX IX_UserMessage_Active_CreatedAt
ON [dbo].[UserMessage] (Active, CreatedAt DESC);
GO
