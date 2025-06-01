CREATE TABLE [dbo].[Suggestion]
(
	
	[Id] INT IDENTITY,
	[User_Id] INT NOT NULL,
	[DateSuggestion] DATE,
	[OriginalPlace] NVARCHAR(64),
	[SuggestedAlternatives] NVARCHAR(256),
	[Reason] NVARCHAR(256),
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_Suggestion] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Suggestion_User] FOREIGN KEY (User_Id) REFERENCES [User] ([Id])
)

GO

CREATE TRIGGER [dbo].[OnDeleteSuggestion]
	ON [dbo].[Suggestion]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE Suggestion SET Active = 0
		WHERE Id = (SELECT Id FROM deleted)
	END
