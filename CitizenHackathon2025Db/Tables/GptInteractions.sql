CREATE TABLE [dbo].[GptInteractions]
(
	[Id] INT IDENTITY,
	[Prompt] NVARCHAR(MAX),
	[PromptHash] NVARCHAR(64) NOT NULL,
	[Response] NVARCHAR(MAX),
	[CreatedAt] DATETIME DEFAULT GETDATE(),
	[DateDeleted] DATETIME2(0) NULL,
	[Active] BIT DEFAULT 1,

	CONSTRAINT [PK_GptInteractions] PRIMARY KEY ([Id] ASC),
)	

GO

CREATE TRIGGER [dbo].[OnDeleteGptInteractions]
	ON [dbo].[GptInteractions]
	INSTEAD OF DELETE
	AS
	BEGIN
		SET NOCOUNT ON;

    UPDATE GptInteraction
    SET Active = 0,
        DateDeleted = SYSUTCDATETIME()
    FROM dbo.GptInteractions GptInteraction
    INNER JOIN deleted d ON d.Id = GptInteraction.Id;
	END

CREATE INDEX IX_GptInteractions_Active ON GptInteractions(Active);

GO

    CREATE UNIQUE INDEX UX_GptInteractions_Active_PromptHash
    ON dbo.GptInteractions(PromptHash)
    WHERE Active = 1;

GO













































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.