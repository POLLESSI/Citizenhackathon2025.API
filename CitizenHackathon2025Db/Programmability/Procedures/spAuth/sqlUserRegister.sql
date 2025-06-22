CREATE PROCEDURE [dbo].[sqlUserRegister]
	@Email NVARCHAR(64),
    @Password NVARCHAR(64),
    @Role NVARCHAR(16) = 'User' -- Default role is 'User'
AS
BEGIN
    DECLARE @PasswordHash BINARY(64), @SecurityStamp UNIQUEIDENTIFIER;

    SET @SecurityStamp = NEWID();
    SET @PasswordHash = dbo.fHasher(TRIM(@Password), @SecurityStamp);

     INSERT INTO [User] (Email, PasswordHash, SecurityStamp, Role, Status)
    VALUES (TRIM(@Email), @PasswordHash, @SecurityStamp, @Role, 0);
END
























































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.