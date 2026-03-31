using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddWorkflowAndApprovalEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowSteps",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RequiredRole = table.Column<int>(type: "int", nullable: false),
                    RequiredCommitteeRole = table.Column<int>(type: "int", nullable: false),
                    StepNameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StepNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WorkflowStepDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlaHours = table.Column<int>(type: "int", nullable: true),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionCommitteeMembers",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CommitteeRole = table.Column<int>(type: "int", nullable: false),
                    ActiveFromPhase = table.Column<int>(type: "int", nullable: true),
                    ActiveToPhase = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionCommitteeMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionPermissionMatrices",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    CommitteeRole = table.Column<int>(type: "int", nullable: false),
                    SystemRole = table.Column<int>(type: "int", nullable: false),
                    AllowedActions = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Competition"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionPermissionMatrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhaseTransitionHistories",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    FromPhase = table.Column<int>(type: "int", nullable: false),
                    ToPhase = table.Column<int>(type: "int", nullable: false),
                    TransitionedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TransitionedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseTransitionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TransitionFrom = table.Column<int>(type: "int", nullable: false),
                    TransitionTo = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepDefinitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RequiredSystemRole = table.Column<int>(type: "int", nullable: false),
                    RequiredCommitteeRole = table.Column<int>(type: "int", nullable: false),
                    StepNameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StepNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SlaHours = table.Column<int>(type: "int", nullable: true),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConditionExpression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepDefinitions_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_CompetitionId",
                schema: "rfp",
                table: "ApprovalWorkflowSteps",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_CompetitionStatus",
                schema: "rfp",
                table: "ApprovalWorkflowSteps",
                columns: new[] { "CompetitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_TenantId",
                schema: "rfp",
                table: "ApprovalWorkflowSteps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_Transition",
                schema: "rfp",
                table: "ApprovalWorkflowSteps",
                columns: new[] { "CompetitionId", "FromStatus", "ToStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionCommitteeMembers_CompetitionId",
                schema: "rfp",
                table: "CompetitionCommitteeMembers",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionCommitteeMembers_Lookup",
                schema: "rfp",
                table: "CompetitionCommitteeMembers",
                columns: new[] { "CompetitionId", "UserId", "CommitteeRole" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionCommitteeMembers_UserId",
                schema: "rfp",
                table: "CompetitionCommitteeMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionMatrix_Lookup",
                schema: "rfp",
                table: "CompetitionPermissionMatrices",
                columns: new[] { "Phase", "CommitteeRole", "SystemRole", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionMatrix_TenantId",
                schema: "rfp",
                table: "CompetitionPermissionMatrices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseTransitionHistories_CompetitionId",
                schema: "rfp",
                table: "PhaseTransitionHistories",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseTransitionHistories_TenantId",
                schema: "rfp",
                table: "PhaseTransitionHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseTransitionHistories_TransitionedAt",
                schema: "rfp",
                table: "PhaseTransitionHistories",
                column: "TransitionedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_TenantId",
                schema: "workflow",
                table: "WorkflowDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_TransitionLookup",
                schema: "workflow",
                table: "WorkflowDefinitions",
                columns: new[] { "TenantId", "TransitionFrom", "TransitionTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDefinitions_Order",
                schema: "workflow",
                table: "WorkflowStepDefinitions",
                columns: new[] { "WorkflowDefinitionId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDefinitions_WorkflowDefinitionId",
                schema: "workflow",
                table: "WorkflowStepDefinitions",
                column: "WorkflowDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalWorkflowSteps",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "CompetitionCommitteeMembers",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "CompetitionPermissionMatrices",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "PhaseTransitionHistories",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "WorkflowStepDefinitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: "workflow");
        }
    }
}
