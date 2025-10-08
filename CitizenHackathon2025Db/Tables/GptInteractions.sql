CREATE TABLE [dbo].[GptInteractions]
(
	[Id] INT IDENTITY,
	[Prompt] NVARCHAR(MAX),
	[PromptHash] NVARCHAR(64) NOT NULL UNIQUE,
	[Response] NVARCHAR(MAX),
	[CreatedAt] DATETIME DEFAULT GETDATE(),
	[Active] BIT DEFAULT 1,

	CONSTRAINT [PK_GptInteractions] PRIMARY KEY ([Id] ASC),
	CONSTRAINT [UQ_GptInteractions_PromptHash] UNIQUE NONCLUSTERED ([PromptHash] ASC)
)

GO

CREATE TRIGGER [dbo].[OnDeleteGptInteractions]
	ON [dbo].[GptInteractions]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE GptInteractions SET Active = 0,
		CreatedAt = GETDATE()
		WHERE Id = (SELECT Id FROM deleted)
	END

CREATE INDEX IX_GptInteractions_Active ON GptInteractions(Active);













































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.