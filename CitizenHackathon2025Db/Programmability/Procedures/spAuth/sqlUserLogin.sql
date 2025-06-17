CREATE PROCEDURE [dbo].[sqlUserLogin]
	@Email NVARCHAR(64),
    @Password NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PasswordHash BINARY(64), @SecurityStamp UNIQUEIDENTIFIER;
    SELECT @SecurityStamp = SecurityStamp FROM [User] WHERE Email = @Email;
    IF @SecurityStamp IS NULL

    BEGIN
        RETURN -1;
    END

    SET @PasswordHash = dbo.fHasher(@Password, @SecurityStamp);

    IF EXISTS (
        SELECT 1 FROM [User]
        WHERE Email = @Email AND PasswordHash = @PasswordHash AND Active = 1
    )
    BEGIN
        SELECT Id, Email, Role, Active 
        FROM [User]
        WHERE Email = @Email;
        RETURN 1;
    END
    ELSE
    BEGIN
        RETURN 0; 
    END
END





















































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.