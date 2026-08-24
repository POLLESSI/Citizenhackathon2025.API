CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Email] NVARCHAR(64) NOT NULL,
    [PasswordHashV2] NVARCHAR(512) NOT NULL,        
    [SecurityStamp] UNIQUEIDENTIFIER NOT NULL,  -- used to invalidate tokens
    [Role] INT NOT NULL  CONSTRAINT DF_Users_Role DEFAULT 0,              -- UserRole (enum : 0=User, 1=Admin, 2=Modo, 4=Guest)
    [Status] INT NOT NULL,                      -- Status (ex: 0=Inactive, 1=Active, 2=Banned ?)
    [Active] BIT NOT NULL CONSTRAINT DF_Users_Active DEFAULT 1,  
    -- Soft delete
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email]),
    CONSTRAINT [UQ_Users_SecurityStamp] UNIQUE ([SecurityStamp]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (0,1,2,4))  -- consistency with enum UserRole
);
GO

CREATE TRIGGER [dbo].[OnDeleteUser]
ON [dbo].[Users]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[Users]
    SET Active = 0
    WHERE Id IN (SELECT Id FROM deleted);
    
END;
GO












































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.