CREATE TABLE [dbo].[GptInteractions]
(
	[Id] INT IDENTITY,
	[Prompt] NVARCHAR(MAX),
	[PromptHash] NVARCHAR(64),
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

    UPDATE G
    SET Active      = 0,
        DateDeleted = SYSUTCDATETIME()
    FROM dbo.GptInteractions AS G
    INNER JOIN deleted d ON d.Id = G.Id;
END;
GO














































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.