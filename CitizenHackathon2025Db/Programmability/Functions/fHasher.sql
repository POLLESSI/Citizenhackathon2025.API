﻿CREATE FUNCTION [dbo].[fHasher]
(
	@Password NVARCHAR(128),
    @SecurityStamp UNIQUEIDENTIFIER
)
RETURNS BINARY(64)
AS
BEGIN
    DECLARE @combined NVARCHAR(164) = @Password + CONVERT(NVARCHAR(36), @SecurityStamp)
    RETURN HASHBYTES('SHA2_512', @combined)
END

































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.