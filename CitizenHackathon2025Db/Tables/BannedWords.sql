CREATE TABLE [dbo].[BannedWords]
(
	[Id] INT IDENTITY PRIMARY KEY,
    [Word] NVARCHAR(100),
    [Severity] INT,
    [Active] BIT
)
