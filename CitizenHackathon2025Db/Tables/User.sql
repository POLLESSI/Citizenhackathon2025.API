CREATE TABLE [dbo].[User]
(
	[Id] INT IDENTITY,
	[Email] NVARCHAR(64),
	[PasswordHash] BINARY(64) NOT NULL,
	[SecurityStamp] UNIQUEIDENTIFIER NOT NULL,
	[Role] NVARCHAR(16),
	[Status] INT NOT NULL,
	[Active] BIT DEFAULT 1


	CONSTRAINT [PK_User] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_User_Email] UNIQUE ([Email]),
	CONSTRAINT [UQ_User_SecurityStamp] UNIQUE ([SecurityStamp]),
	CONSTRAINT [UQ_User_Role] UNIQUE ([Role])
)

GO

CREATE TRIGGER [dbo].[OnDeleteUser]
	ON [dbo].[User]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE [User] SET Active = 0
		WHERE Id = (SELECT Id FROM deleted)
	END













































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.