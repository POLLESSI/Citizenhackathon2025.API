CREATE PROCEDURE dbo.sp_GptInteraction_Upsert
    @Prompt NVARCHAR(MAX),
    @PromptHash NVARCHAR(64),
    @Response NVARCHAR(MAX),
    @Model NVARCHAR(100) = NULL,
    @Temperature FLOAT = NULL,
    @TokenCount INT = NULL
AS
BEGIN
    MERGE [dbo].[GptInteractions] WITH (HOLDLOCK) AS t
    USING (SELECT @PromptHash AS PromptHash) AS s
    ON (t.PromptHash = s.PromptHash)
    WHEN MATCHED THEN
        UPDATE SET
            Response = @Response,
            CreatedAt = SYSUTCDATETIME(),
            Active = 1,
            Model = ISNULL(@Model, t.Model),
            Temperature = ISNULL(@Temperature, t.Temperature),
            TokenCount = ISNULL(@TokenCount, t.TokenCount)
    WHEN NOT MATCHED THEN
        INSERT (Prompt, PromptHash, Response, CreatedAt, Active, Model, Temperature, TokenCount)
        VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1, @Model, @Temperature, @TokenCount);

    SELECT * FROM [GptInteractions] WHERE PromptHash = @PromptHash;
END









































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.