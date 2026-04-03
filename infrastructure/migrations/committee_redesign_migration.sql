-- ============================================================
-- Migration: Committee System Redesign
-- Date: 2026-04-03
-- Description: Add ScopeType, create CommitteeCompetitions table,
--              migrate CompetitionId data, and clean up old column.
-- ============================================================

BEGIN TRANSACTION;

-- Step 1: Add ScopeType column to Committees
-- Default = 1 (Comprehensive) for existing committees
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Committees' AND COLUMN_NAME = 'ScopeType'
)
BEGIN
    ALTER TABLE [Committees] ADD [ScopeType] INT NOT NULL DEFAULT 1;
    PRINT 'Added ScopeType column to Committees';
END
ELSE
BEGIN
    PRINT 'ScopeType column already exists';
END

-- Step 2: Create CommitteeCompetitions table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'CommitteeCompetitions'
)
BEGIN
    CREATE TABLE [CommitteeCompetitions] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [CommitteeId] UNIQUEIDENTIFIER NOT NULL,
        [CompetitionId] UNIQUEIDENTIFIER NOT NULL,
        [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [AssignedBy] NVARCHAR(256) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(256) NULL,
        [LastModifiedAt] DATETIME2 NULL,
        [LastModifiedBy] NVARCHAR(256) NULL,
        CONSTRAINT [PK_CommitteeCompetitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommitteeCompetitions_Committees_CommitteeId]
            FOREIGN KEY ([CommitteeId]) REFERENCES [Committees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_CommitteeCompetitions_Committee_Competition]
            UNIQUE ([CommitteeId], [CompetitionId])
    );

    CREATE INDEX [IX_CommitteeCompetitions_CommitteeId]
        ON [CommitteeCompetitions] ([CommitteeId]);

    CREATE INDEX [IX_CommitteeCompetitions_CompetitionId]
        ON [CommitteeCompetitions] ([CompetitionId]);

    PRINT 'Created CommitteeCompetitions table';
END
ELSE
BEGIN
    PRINT 'CommitteeCompetitions table already exists';
END

-- Step 3: Migrate existing CompetitionId data to CommitteeCompetitions
-- For committees that have a CompetitionId, create a record in CommitteeCompetitions
INSERT INTO [CommitteeCompetitions] ([Id], [CommitteeId], [CompetitionId], [AssignedAt], [AssignedBy], [CreatedAt], [CreatedBy])
SELECT
    NEWID(),
    c.[Id],
    c.[CompetitionId],
    GETUTCDATE(),
    c.[CreatedBy],
    GETUTCDATE(),
    c.[CreatedBy]
FROM [Committees] c
WHERE c.[CompetitionId] IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM [CommitteeCompetitions] cc
    WHERE cc.[CommitteeId] = c.[Id] AND cc.[CompetitionId] = c.[CompetitionId]
);

DECLARE @MigratedCount INT = @@ROWCOUNT;
PRINT 'Migrated ' + CAST(@MigratedCount AS VARCHAR(10)) + ' competition links';

-- Step 4: Update ScopeType for committees that had a CompetitionId
-- If they had ActiveFromPhase/ActiveToPhase, set to SpecificPhasesSpecificCompetitions (3)
-- If they had CompetitionId but no phases, set to Comprehensive (1)
UPDATE [Committees]
SET [ScopeType] = CASE
    WHEN [ActiveFromPhase] IS NOT NULL AND [ActiveToPhase] IS NOT NULL AND [CompetitionId] IS NOT NULL
        THEN 3 -- SpecificPhasesSpecificCompetitions
    WHEN [ActiveFromPhase] IS NOT NULL AND [ActiveToPhase] IS NOT NULL
        THEN 2 -- SpecificPhasesAllCompetitions
    ELSE 1 -- Comprehensive
END;

PRINT 'Updated ScopeType for existing committees';

-- Step 5: Drop CompetitionId column (no longer needed, data migrated)
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Committees' AND COLUMN_NAME = 'CompetitionId'
)
BEGIN
    -- Drop any existing foreign key constraints on CompetitionId
    DECLARE @fkName NVARCHAR(256);
    SELECT @fkName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE OBJECT_NAME(fk.parent_object_id) = 'Committees' AND c.name = 'CompetitionId';

    IF @fkName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [Committees] DROP CONSTRAINT [' + @fkName + ']');
        PRINT 'Dropped FK constraint: ' + @fkName;
    END

    -- Drop any existing indexes on CompetitionId
    DECLARE @idxName NVARCHAR(256);
    SELECT @idxName = i.name
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE OBJECT_NAME(i.object_id) = 'Committees' AND c.name = 'CompetitionId' AND i.is_primary_key = 0;

    IF @idxName IS NOT NULL
    BEGIN
        EXEC('DROP INDEX [' + @idxName + '] ON [Committees]');
        PRINT 'Dropped index: ' + @idxName;
    END

    ALTER TABLE [Committees] DROP COLUMN [CompetitionId];
    PRINT 'Dropped CompetitionId column from Committees';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '=== Committee Redesign Migration Complete ===';
PRINT '';
