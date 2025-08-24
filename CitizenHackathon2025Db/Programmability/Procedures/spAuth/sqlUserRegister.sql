CREATE PROCEDURE [dbo].[sqlUserRegister]
    @Email NVARCHAR(64),
    @Password NVARCHAR(64),
    @Role INT = 0  -- Default role = User
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PasswordHash BINARY(64), @SecurityStamp UNIQUEIDENTIFIER;
    SET @SecurityStamp = NEWID();

    -- Appelle ta fonction de hashage
    SET @PasswordHash = dbo.fHasher(TRIM(@Password), @SecurityStamp);

    INSERT INTO [Users] (Email, PasswordHash, SecurityStamp, Role, Status, Active)
    VALUES (TRIM(@Email), @PasswordHash, @SecurityStamp, @Role, 0, 1);
END
GO























































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.