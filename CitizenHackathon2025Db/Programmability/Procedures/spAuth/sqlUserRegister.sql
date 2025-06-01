CREATE PROCEDURE [dbo].[sqlUserRegister]
	@Email NVARCHAR(64),
    @Password NVARCHAR(64)
AS
BEGIN
    DECLARE @PasswordHash BINARY(64), @SecurityStamp UNIQUEIDENTIFIER;

    SET @SecurityStamp = NEWID();
    SET @PasswordHash = dbo.fHasher(TRIM(@Password), @SecurityStamp);

    INSERT INTO [User] (Email, PasswordHash, SecurityStamp)
    VALUES (TRIM(@Email), @PasswordHash, @SecurityStamp);
END
