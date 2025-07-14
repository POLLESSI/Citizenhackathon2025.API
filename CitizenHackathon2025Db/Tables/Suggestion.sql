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
		WHERE Id IN (SELECT Id FROM deleted)
	END

ALTER TABLE [dbo].[Suggestion]
ADD EventId INT NULL,
    ForecastId INT NULL,
    TrafficId INT NULL;

ALTER TABLE [dbo].[Suggestion]
ADD CONSTRAINT FK_Suggestion_Event FOREIGN KEY (EventId) REFERENCES [Event](Id);

ALTER TABLE [dbo].[Suggestion]
ADD CONSTRAINT FK_Suggestion_WeatherForecast FOREIGN KEY (ForecastId) REFERENCES [WeatherForecast](Id);

ALTER TABLE [dbo].[Suggestion]
ADD CONSTRAINT FK_Suggestion_Traffic FOREIGN KEY (TrafficId) REFERENCES [TrafficCondition](Id);

ALTER TABLE Suggestion ADD LocationName NVARCHAR(256)

CREATE INDEX IX_Suggestion_EventId ON Suggestion(EventId);

CREATE INDEX IX_Suggestion_ForecastId ON Suggestion(ForecastId);

CREATE INDEX IX_Suggestion_TrafficId ON Suggestion(TrafficId);


















































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.