-- MigrationScript.sql
-- Migrating the Role column (text → INT) for the Users table
-- Compatible with re-execution (idempotent)

BEGIN TRANSACTION;

-- 1️⃣ Add the new temporary column if it doesn't already exist
IF COL_LENGTH('dbo.Users', 'RoleInt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD RoleInt INT NULL;
END;

-- 2️⃣ If the old Role column is of type NVARCHAR, migrate the data
IF EXISTS (
    SELECT 1 
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Users')
      AND c.name = 'Role'
      AND t.name IN ('nvarchar', 'varchar')
)
BEGIN
    UPDATE [dbo].[Users]
    SET RoleInt = CASE Role
        WHEN 'User'  THEN 0
        WHEN 'Admin' THEN 1
        WHEN 'Modo'  THEN 2
        WHEN 'Guest' THEN 4
        ELSE 0
    END;
END;

-- 3️⃣ Delete the old UNIQUE constraint if it exists
IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[UQ_Users_Role]')
      AND type = 'UQ'
)
BEGIN
    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [UQ_Users_Role];
END;

-- 4️⃣ Delete the old Role column if it is in text
IF EXISTS (
    SELECT 1 
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Users')
      AND c.name = 'Role'
      AND t.name IN ('nvarchar', 'varchar')
)
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN Role;
END;

-- 5️⃣ Rename RoleInt → Role (if not already done)
IF COL_LENGTH('dbo.Users', 'RoleInt') IS NOT NULL
   AND COL_LENGTH('dbo.Users', 'Role') IS NULL
BEGIN
    EXEC sp_rename 'Users.RoleInt', 'Role', 'COLUMN';
END;

-- 6️⃣ Add the CHECK constraint (only if it doesn't already exist)
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Users_Role'
      AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT CK_Users_Role CHECK (Role IN (0,1,2,4));
END;

COMMIT TRANSACTION;
GO