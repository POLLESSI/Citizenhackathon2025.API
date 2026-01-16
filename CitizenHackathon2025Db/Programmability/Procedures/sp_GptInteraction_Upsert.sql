CREATE PROCEDURE dbo.sp_GptInteraction_Upsert
    @Prompt      NVARCHAR(MAX),
    @PromptHash  NVARCHAR(64),
    @Response    NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

    MERGE dbo.GptInteractions AS T
    USING (SELECT @PromptHash AS PromptHash) AS S
        ON (T.PromptHash = S.PromptHash)
    WHEN MATCHED THEN
        UPDATE SET 
            T.Prompt      = @Prompt,
            T.Response    = @Response,
            T.CreatedAt   = @Now,
            T.Active      = 1,
            T.DateDeleted = NULL
    WHEN NOT MATCHED THEN
        INSERT (Prompt, PromptHash, Response, CreatedAt, Active)
        VALUES (@Prompt, @PromptHash, @Response, @Now, 1);

    -- Returns the record for the C# mapping
    SELECT TOP (1) *
    FROM dbo.GptInteractions
    WHERE PromptHash = @PromptHash;
END;
GO








































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.