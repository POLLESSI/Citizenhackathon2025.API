CREATE TABLE [dbo].[Suggestion]
(
	
	[Id] INT IDENTITY,
	[User_Id] INT NOT NULL,
	[DateSuggestion] DATE,
	[OriginalPlace] NVARCHAR(64),
	[SuggestedAlternatives] NVARCHAR(256),
	[Reason] NVARCHAR(256),
	[Active] BIT DEFAULT 1,
	[DateDeleted] DATETIME NULL,

	CONSTRAINT [PK_Suggestion] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Suggestion_User] FOREIGN KEY (User_Id) REFERENCES [User] ([Id])
)

GO

CREATE TRIGGER [dbo].[OnDeleteSuggestion]
	ON [dbo].[Suggestion]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE Suggestion SET Active = 0,
		DateDeleted = GETDATE()
		WHERE Id = (SELECT Id FROM deleted)
	END




















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.