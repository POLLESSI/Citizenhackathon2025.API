--sql/01_fix_gpt_indexes.sql

/* ===================================================================
   01_fix_gpt_indexes.sql — GptInteractions indexes cleanup (idempotent)
   - Removes any global UNIQUE on PromptHash (constraint or index)
   - Recreates IX_GptInteractions_Active (non-unique)
   - Recreates the unique filtered UX_GptInteractions_Active_PromptHash
   - Blocks creation if active duplicates exist
   =================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.GptInteractions', N'U') IS NULL
BEGIN
    PRINT N'[01_fix_gpt_indexes] Table dbo.GptInteractions absente — rien à faire.';
    RETURN;
END

/* ---------------------------------------------------------------
   1) Drop global UNIQUEs on PromptHash (OR index constraint)
   --------------------------------------------------------------- */
BEGIN TRY
    -- a) UNIQUE constraint (UQ_...) on PromptHash, unfiltered
    DECLARE @sql NVARCHAR(MAX) = N'';
    ;WITH uq AS (
        SELECT kc.[name] AS uq_name, t.[name] AS table_name
        FROM sys.key_constraints kc
        JOIN sys.tables t ON t.object_id = kc.parent_object_id
        WHERE kc.[type] = 'UQ'
          AND kc.parent_object_id = OBJECT_ID(N'dbo.GptInteractions')
          AND kc.unique_index_id IS NOT NULL
    ),
    uq_cols AS (
        SELECT u.uq_name, c.[name] AS col_name
        FROM uq u
        JOIN sys.indexes i
          ON i.object_id = OBJECT_ID(N'dbo.GptInteractions')
         AND i.[name] = u.uq_name
        JOIN sys.index_columns ic
          ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c
          ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.has_filter = 0
    )
    SELECT @sql = STRING_AGG(CONVERT(NVARCHAR(MAX),
                N'ALTER TABLE dbo.GptInteractions DROP CONSTRAINT [' + uq_name + N'];'
            ), CHAR(10))
    FROM (
        SELECT DISTINCT uq_name
        FROM uq_cols
        GROUP BY uq_name
        HAVING MIN(CASE WHEN col_name = N'PromptHash' THEN 1 ELSE 0 END) = 1
           AND MAX(CASE WHEN col_name <> N'PromptHash' THEN 1 ELSE 0 END) = 0
    ) d;

    IF (@sql IS NOT NULL AND @sql <> N'')
    BEGIN
        PRINT N'[01_fix_gpt_indexes] Drop UQ constraint(s) on PromptHash (global).';
        EXEC sp_executesql @sql;
    END

    -- b) Unfiltered UNIQUE index on PromptHash (if not created via constraint)
    DECLARE @idx_sql NVARCHAR(MAX) = N'';
    ;WITH i_prompt AS (
        SELECT i.[name] AS idx_name
        FROM sys.indexes i
        JOIN sys.index_columns ic
          ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c
          ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.GptInteractions')
          AND i.is_unique = 1
          AND i.has_filter = 0
        GROUP BY i.[name]
        HAVING MIN(CASE WHEN c.[name] = N'PromptHash' THEN 1 ELSE 0 END) = 1
           AND MAX(CASE WHEN c.[name] <> N'PromptHash' THEN 1 ELSE 0 END) = 0
    )
    SELECT @idx_sql = STRING_AGG(CONVERT(NVARCHAR(MAX),
                N'DROP INDEX [' + idx_name + N'] ON dbo.GptInteractions;'
            ), CHAR(10))
    FROM i_prompt;

    IF (@idx_sql IS NOT NULL AND @idx_sql <> N'')
    BEGIN
        PRINT N'[01_fix_gpt_indexes] Drop UNIQUE index(es) on PromptHash (global).';
        EXEC sp_executesql @idx_sql;
    END
END TRY
BEGIN CATCH
    PRINT N'[01_fix_gpt_indexes] Warning: drop UQ/index PromptHash — ' + ERROR_MESSAGE();
END CATCH
GO

/* ---------------------------------------------------------------
   2) (Re)creation of IX_GptInteractions_Active (non-unique)
   --------------------------------------------------------------- */
IF EXISTS (SELECT 1 FROM sys.indexes 
           WHERE [name] = N'IX_GptInteractions_Active'
             AND [object_id] = OBJECT_ID(N'dbo.GptInteractions'))
BEGIN
    PRINT N'[01_fix_gpt_indexes] Drop IX_GptInteractions_Active (recreate clean).';
    DROP INDEX [IX_GptInteractions_Active] ON dbo.GptInteractions;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE [name] = N'IX_GptInteractions_Active'
                 AND [object_id] = OBJECT_ID(N'dbo.GptInteractions'))
BEGIN
    PRINT N'[01_fix_gpt_indexes] Create IX_GptInteractions_Active.';
    CREATE INDEX [IX_GptInteractions_Active]
    ON dbo.GptInteractions([Active]);
END
GO

/* ---------------------------------------------------------------
   3) UNIQUE filtered on PromptHash, Active = 1
- We don't (re)create if an equivalent filtered index already exists,
even with a different name.
- We block if active duplicates are detected.
   --------------------------------------------------------------- */

-- 3.a) Check if an equivalent filtered index already exists
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic
      ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c
      ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.GptInteractions')
      AND i.is_unique = 1
      AND i.has_filter = 1
      AND c.[name] = N'PromptHash'
      AND (i.filter_definition LIKE N'%[Active]% = 1%' OR i.filter_definition LIKE N'%Active=1%')
    GROUP BY i.index_id
    HAVING COUNT(*) = 1
)
BEGIN
    -- 3.b) Check active duplicates before creation
    IF EXISTS (
        SELECT PromptHash
        FROM dbo.GptInteractions
        WHERE Active = 1 AND PromptHash IS NOT NULL
        GROUP BY PromptHash
        HAVING COUNT(*) > 1
    )
    BEGIN
        RAISERROR(N'[01_fix_gpt_indexes] Des doublons Active=1 existent sur PromptHash. Corrigez les données avant de créer l''index unique filtré.', 11, 1);
        RETURN;
    END

    -- 3.c) Create the expected filtered unique (canonical name)
    PRINT N'[01_fix_gpt_indexes] Create UNIQUE filtered index UX_GptInteractions_Active_PromptHash.';
    CREATE UNIQUE INDEX [UX_GptInteractions_Active_PromptHash]
      ON dbo.GptInteractions([PromptHash])
      WHERE [Active] = 1;
END
ELSE
BEGIN
    PRINT N'[01_fix_gpt_indexes] Un index UNIQUE filtré équivalent existe déjà — aucune action.';
END
GO























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.