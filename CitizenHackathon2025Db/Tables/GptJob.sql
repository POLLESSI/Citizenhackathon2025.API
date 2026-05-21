CREATE TABLE [dbo].[GptJob]
(
	[Id] INT IDENTITY(1,1) PRIMARY KEY,
    [InteractionId] INT NOT NULL,
    [PromptHash] NVARCHAR(64) NOT NULL,
    [Prompt] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(32) NOT NULL DEFAULT 'Pending',
    [Attempts] INT NOT NULL DEFAULT 0,
    [CreatedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [StartedAtUtc] DATETIME2(3) NULL,
    [CompletedAtUtc] DATETIME2(3) NULL,
    [ErrorMessage] NVARCHAR(512) NULL
);
