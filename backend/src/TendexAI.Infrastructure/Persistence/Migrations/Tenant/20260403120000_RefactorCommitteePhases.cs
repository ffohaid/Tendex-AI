using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Refactors the Committee table from range-based phase model (ActiveFromPhase/ActiveToPhase)
/// to multi-select phase model (PhasesString) and adds ScopeType, CommitteeCompetitions table.
/// Also removes old per-member phase columns from CommitteeMembers.
///
/// Changes:
/// 1. Committees: Drop ActiveFromPhase, ActiveToPhase, CompetitionId columns
/// 2. Committees: Add ScopeType (int, default 1=Comprehensive), PhasesString (nvarchar(100), nullable)
/// 3. Committees: Drop old indexes on CompetitionId
/// 4. Committees: Add new index on (TenantId, ScopeType)
/// 5. CommitteeMembers: Drop ActiveFromPhase, ActiveToPhase columns
/// 6. Create new CommitteeCompetitions join table
/// 7. Data migration: Convert old range-based phases to comma-separated string
/// </summary>
public partial class RefactorCommitteePhases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ─────────────────────────────────────────────────────────
        // Step 1: Add new columns to Committees BEFORE dropping old ones
        // ─────────────────────────────────────────────────────────

        migrationBuilder.AddColumn<int>(
            name: "ScopeType",
            schema: "committees",
            table: "Committees",
            type: "int",
            nullable: false,
            defaultValue: 1); // Comprehensive

        migrationBuilder.AddColumn<string>(
            name: "PhasesString",
            schema: "committees",
            table: "Committees",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        // ─────────────────────────────────────────────────────────
        // Step 2: Migrate existing data — convert range to comma-separated
        // ─────────────────────────────────────────────────────────

        // For committees that have ActiveFromPhase and ActiveToPhase set,
        // generate a comma-separated list of phase values.
        // Set ScopeType = 2 (SpecificPhasesAllCompetitions) if they had a CompetitionId,
        // otherwise keep as Comprehensive.
        migrationBuilder.Sql(@"
            -- Convert phase range to comma-separated string
            ;WITH PhaseRange AS (
                SELECT
                    c.Id,
                    c.ActiveFromPhase,
                    c.ActiveToPhase,
                    c.CompetitionId,
                    v.Number AS PhaseValue
                FROM committees.Committees c
                CROSS APPLY (
                    SELECT TOP (ISNULL(c.ActiveToPhase, 9) - ISNULL(c.ActiveFromPhase, 1) + 1)
                        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) + ISNULL(c.ActiveFromPhase, 1) - 1 AS Number
                    FROM sys.objects
                ) v
                WHERE c.ActiveFromPhase IS NOT NULL
                  AND c.ActiveToPhase IS NOT NULL
            )
            UPDATE c
            SET c.PhasesString = (
                SELECT STRING_AGG(CAST(pr.PhaseValue AS NVARCHAR(10)), ',') WITHIN GROUP (ORDER BY pr.PhaseValue)
                FROM PhaseRange pr
                WHERE pr.Id = c.Id
            ),
            c.ScopeType = CASE
                WHEN c.CompetitionId IS NOT NULL THEN 3  -- SpecificPhasesSpecificCompetitions
                ELSE 2  -- SpecificPhasesAllCompetitions
            END
            FROM committees.Committees c
            WHERE c.ActiveFromPhase IS NOT NULL
              AND c.ActiveToPhase IS NOT NULL;

            -- Committees without phase range stay as Comprehensive (ScopeType = 1)
            UPDATE committees.Committees
            SET ScopeType = 1
            WHERE ActiveFromPhase IS NULL AND ActiveToPhase IS NULL;
        ");

        // ─────────────────────────────────────────────────────────
        // Step 3: Create CommitteeCompetitions join table
        // ─────────────────────────────────────────────────────────

        migrationBuilder.CreateTable(
            name: "CommitteeCompetitions",
            schema: "committees",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                AssignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommitteeCompetitions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CommitteeCompetitions_Committees_CommitteeId",
                    column: x => x.CommitteeId,
                    principalSchema: "committees",
                    principalTable: "Committees",
                    principalColumn: "Id");
            });

        // ─────────────────────────────────────────────────────────
        // Step 4: Migrate existing CompetitionId data to join table
        // ─────────────────────────────────────────────────────────

        migrationBuilder.Sql(@"
            INSERT INTO committees.CommitteeCompetitions
                (Id, CommitteeId, CompetitionId, AssignedAt, AssignedBy, CreatedAt, CreatedBy)
            SELECT
                NEWID(),
                c.Id,
                c.CompetitionId,
                c.CreatedAt,
                ISNULL(c.CreatedBy, 'system'),
                GETUTCDATE(),
                'migration'
            FROM committees.Committees c
            WHERE c.CompetitionId IS NOT NULL;
        ");

        // ─────────────────────────────────────────────────────────
        // Step 5: Create indexes for CommitteeCompetitions
        // ─────────────────────────────────────────────────────────

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeCompetitions_CommitteeId",
            schema: "committees",
            table: "CommitteeCompetitions",
            column: "CommitteeId");

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeCompetitions_CompetitionId",
            schema: "committees",
            table: "CommitteeCompetitions",
            column: "CompetitionId");

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeCompetitions_CommitteeId_CompetitionId",
            schema: "committees",
            table: "CommitteeCompetitions",
            columns: new[] { "CommitteeId", "CompetitionId" },
            unique: true);

        // ─────────────────────────────────────────────────────────
        // Step 6: Drop old indexes from Committees
        // ─────────────────────────────────────────────────────────

        migrationBuilder.DropIndex(
            name: "IX_Committees_CompetitionId",
            schema: "committees",
            table: "Committees");

        migrationBuilder.DropIndex(
            name: "IX_Committees_CompetitionId_Type",
            schema: "committees",
            table: "Committees");

        // ─────────────────────────────────────────────────────────
        // Step 7: Drop old columns from Committees
        // ─────────────────────────────────────────────────────────

        migrationBuilder.DropColumn(
            name: "ActiveFromPhase",
            schema: "committees",
            table: "Committees");

        migrationBuilder.DropColumn(
            name: "ActiveToPhase",
            schema: "committees",
            table: "Committees");

        migrationBuilder.DropColumn(
            name: "CompetitionId",
            schema: "committees",
            table: "Committees");

        // ─────────────────────────────────────────────────────────
        // Step 8: Drop old columns from CommitteeMembers
        // ─────────────────────────────────────────────────────────

        migrationBuilder.DropColumn(
            name: "ActiveFromPhase",
            schema: "committees",
            table: "CommitteeMembers");

        migrationBuilder.DropColumn(
            name: "ActiveToPhase",
            schema: "committees",
            table: "CommitteeMembers");

        // ─────────────────────────────────────────────────────────
        // Step 9: Add new index for ScopeType
        // ─────────────────────────────────────────────────────────

        migrationBuilder.CreateIndex(
            name: "IX_Committees_TenantId_ScopeType",
            schema: "committees",
            table: "Committees",
            columns: new[] { "TenantId", "ScopeType" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ─────────────────────────────────────────────────────────
        // Reverse: Re-add old columns to Committees
        // ─────────────────────────────────────────────────────────

        migrationBuilder.AddColumn<int>(
            name: "ActiveFromPhase",
            schema: "committees",
            table: "Committees",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ActiveToPhase",
            schema: "committees",
            table: "Committees",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CompetitionId",
            schema: "committees",
            table: "Committees",
            type: "uniqueidentifier",
            nullable: true);

        // Re-add old columns to CommitteeMembers
        migrationBuilder.AddColumn<int>(
            name: "ActiveFromPhase",
            schema: "committees",
            table: "CommitteeMembers",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ActiveToPhase",
            schema: "committees",
            table: "CommitteeMembers",
            type: "int",
            nullable: true);

        // Reverse data migration: Convert PhasesString back to range
        migrationBuilder.Sql(@"
            UPDATE c
            SET c.ActiveFromPhase = TRY_CAST(
                    LEFT(c.PhasesString, CHARINDEX(',', c.PhasesString + ',') - 1) AS INT
                ),
                c.ActiveToPhase = TRY_CAST(
                    RIGHT(c.PhasesString, LEN(c.PhasesString) - LEN(c.PhasesString) + LEN(REVERSE(LEFT(REVERSE(c.PhasesString), CHARINDEX(',', REVERSE(c.PhasesString) + ',') - 1)))) AS INT
                ),
                c.CompetitionId = (
                    SELECT TOP 1 cc.CompetitionId
                    FROM committees.CommitteeCompetitions cc
                    WHERE cc.CommitteeId = c.Id
                )
            FROM committees.Committees c
            WHERE c.PhasesString IS NOT NULL;
        ");

        // Re-create old indexes
        migrationBuilder.CreateIndex(
            name: "IX_Committees_CompetitionId",
            schema: "committees",
            table: "Committees",
            column: "CompetitionId");

        migrationBuilder.CreateIndex(
            name: "IX_Committees_CompetitionId_Type",
            schema: "committees",
            table: "Committees",
            columns: new[] { "CompetitionId", "Type" });

        // Drop new index
        migrationBuilder.DropIndex(
            name: "IX_Committees_TenantId_ScopeType",
            schema: "committees",
            table: "Committees");

        // Drop new columns
        migrationBuilder.DropColumn(
            name: "ScopeType",
            schema: "committees",
            table: "Committees");

        migrationBuilder.DropColumn(
            name: "PhasesString",
            schema: "committees",
            table: "Committees");

        // Drop CommitteeCompetitions table
        migrationBuilder.DropTable(
            name: "CommitteeCompetitions",
            schema: "committees");
    }
}
