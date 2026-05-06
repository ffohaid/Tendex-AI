using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Committees_CompetitionId",
                schema: "committees",
                table: "Committees");

            migrationBuilder.DropIndex(
                name: "IX_Committees_CompetitionId_Type",
                schema: "committees",
                table: "Committees");

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

            migrationBuilder.DropColumn(
                name: "ActiveFromPhase",
                schema: "committees",
                table: "CommitteeMembers");

            migrationBuilder.DropColumn(
                name: "ActiveToPhase",
                schema: "committees",
                table: "CommitteeMembers");

            migrationBuilder.EnsureSchema(
                name: "inquiries");

            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "identity",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CommitteeId",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "evaluation",
                table: "SupplierOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "evaluation",
                table: "SupplierOffers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "evaluation",
                table: "SupplierOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProtected",
                schema: "identity",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedAwardDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalYear",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InquiriesStartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InquiryPeriodDays",
                schema: "rfp",
                table: "Competitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OffersStartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredAttachmentTypes",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkStartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhasesString",
                schema: "committees",
                table: "Committees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeType",
                schema: "committees",
                table: "Committees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DeploymentType",
                table: "AiConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AiConfigurations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActiveDirectoryConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false, defaultValue: 389),
                    BaseDn = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    BindDn = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    EncryptedBindPassword = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SearchFilter = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    UseSsl = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UseTls = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UserAttributeMapping = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    GroupAttributeMapping = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastConnectionTestAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastConnectionTestResult = table.Column<bool>(type: "bit", nullable: true),
                    LastConnectionTestError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveDirectoryConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveDirectoryConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

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
                name: "BookletTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookletTemplates", x => x.Id);
                });

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
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
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
                name: "CompetitionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompetitionType = table.Column<int>(type: "int", nullable: false),
                    IsOfficial = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEvaluations",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inquiries",
                schema: "inquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EtimadReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAnswer = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedToCommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnsweredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsAiAssisted = table.Column<bool>(type: "bit", nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsExportedToEtimad = table.Column<bool>(type: "bit", nullable: false),
                    ExportedToEtimadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquiries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionMatrixRules",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<int>(type: "int", nullable: false),
                    CommitteeRole = table.Column<int>(type: "int", nullable: true),
                    CompetitionPhase = table.Column<int>(type: "int", nullable: true),
                    AllowedActions = table.Column<int>(type: "int", nullable: false),
                    IsCustomized = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionMatrixRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionMatrixRules_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id");
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
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AiSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AiSuggestedResolution = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    AiSentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AiSuggestedCategory = table.Column<int>(type: "int", nullable: true),
                    AiSuggestedPriority = table.Column<int>(type: "int", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    SatisfactionRating = table.Column<int>(type: "int", nullable: true),
                    SatisfactionFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
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
                name: "BookletTemplateSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsMainSection = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookletTemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookletTemplateSections_BookletTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "BookletTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateBoqItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBoqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBoqItems_CompetitionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CompetitionTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateEvaluationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateEvaluationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateEvaluationCriteria_CompetitionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CompetitionTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SectionType = table.Column<int>(type: "int", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultTextColor = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateSections_CompetitionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CompetitionTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialOfferItems",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoqItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsArithmeticallyVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasArithmeticError = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SupplierSubmittedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeviationPercentage = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    DeviationLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOfferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialOfferItems_FinancialEvaluations_FinancialEvaluationId",
                        column: x => x.FinancialEvaluationId,
                        principalSchema: "evaluation",
                        principalTable: "FinancialEvaluations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialOfferItems_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialScores",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialScores_FinancialEvaluations_FinancialEvaluationId",
                        column: x => x.FinancialEvaluationId,
                        principalSchema: "evaluation",
                        principalTable: "FinancialEvaluations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialScores_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InquiryResponses",
                schema: "inquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "bit", nullable: false),
                    AiConfidenceScore = table.Column<int>(type: "int", nullable: true),
                    AiModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AiSources = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InquiryResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InquiryResponses_Inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalSchema: "inquiries",
                        principalTable: "Inquiries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOperatorMessage = table.Column<bool>(type: "bit", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "bit", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "BookletTemplateBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorType = table.Column<int>(type: "int", nullable: false),
                    IsHeading = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasBracketPlaceholders = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookletTemplateBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookletTemplateBlocks_BookletTemplateSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "BookletTemplateSections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                column: "ReferenceNumber",
                unique: true,
                filter: "[ReferenceNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId_ScopeType",
                schema: "committees",
                table: "Committees",
                columns: new[] { "TenantId", "ScopeType" });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveDirectoryConfigurations_TenantId",
                table: "ActiveDirectoryConfigurations",
                column: "TenantId",
                unique: true);

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
                name: "IX_BookletTemplateBlocks_SectionId",
                table: "BookletTemplateBlocks",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookletTemplates_Category",
                table: "BookletTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BookletTemplates_IsActive",
                table: "BookletTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BookletTemplates_TenantId",
                table: "BookletTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookletTemplateSections_TemplateId",
                table: "BookletTemplateSections",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeCompetitions_CommitteeId",
                schema: "committees",
                table: "CommitteeCompetitions",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeCompetitions_CommitteeId_CompetitionId",
                schema: "committees",
                table: "CommitteeCompetitions",
                columns: new[] { "CommitteeId", "CompetitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeCompetitions_CompetitionId",
                schema: "committees",
                table: "CommitteeCompetitions",
                column: "CompetitionId");

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
                name: "IX_CompetitionTemplates_Category",
                table: "CompetitionTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTemplates_IsActive",
                table: "CompetitionTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTemplates_TenantId",
                table: "CompetitionTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvaluations_CommitteeId",
                schema: "evaluation",
                table: "FinancialEvaluations",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvaluations_CompetitionId",
                schema: "evaluation",
                table: "FinancialEvaluations",
                column: "CompetitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvaluations_Status",
                schema: "evaluation",
                table: "FinancialEvaluations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvaluations_TechnicalEvaluationId",
                schema: "evaluation",
                table: "FinancialEvaluations",
                column: "TechnicalEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvaluations_TenantId",
                schema: "evaluation",
                table: "FinancialEvaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_BoqItemId",
                schema: "evaluation",
                table: "FinancialOfferItems",
                column: "BoqItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_Eval_Offer_Boq",
                schema: "evaluation",
                table: "FinancialOfferItems",
                columns: new[] { "FinancialEvaluationId", "SupplierOfferId", "BoqItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_SupplierOfferId",
                schema: "evaluation",
                table: "FinancialOfferItems",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_Eval_Offer_Evaluator",
                schema: "evaluation",
                table: "FinancialScores",
                columns: new[] { "FinancialEvaluationId", "SupplierOfferId", "EvaluatorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_SupplierOfferId",
                schema: "evaluation",
                table: "FinancialScores",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_AssignedToUserId",
                schema: "inquiries",
                table: "Inquiries",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_Category",
                schema: "inquiries",
                table: "Inquiries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_CompetitionId",
                schema: "inquiries",
                table: "Inquiries",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_ReferenceNumber",
                schema: "inquiries",
                table: "Inquiries",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_SlaDeadline",
                schema: "inquiries",
                table: "Inquiries",
                column: "SlaDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_Status",
                schema: "inquiries",
                table: "Inquiries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_TenantId",
                schema: "inquiries",
                table: "Inquiries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InquiryResponses_InquiryId",
                schema: "inquiries",
                table: "InquiryResponses",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionMatrixRules_Dimensions",
                schema: "identity",
                table: "PermissionMatrixRules",
                columns: new[] { "TenantId", "RoleId", "Scope", "ResourceType", "CommitteeRole", "CompetitionPhase" },
                unique: true,
                filter: "[CommitteeRole] IS NOT NULL AND [CompetitionPhase] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionMatrixRules_Lookup",
                schema: "identity",
                table: "PermissionMatrixRules",
                columns: new[] { "RoleId", "Scope", "ResourceType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionMatrixRules_TenantId",
                schema: "identity",
                table: "PermissionMatrixRules",
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
                name: "IX_SupportTicketMessages_CreatedAt",
                table: "SupportTicketMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_SupportTicketId",
                table: "SupportTicketMessages",
                column: "SupportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAt",
                table: "SupportTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Priority",
                table: "SupportTickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TenantId",
                table: "SupportTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TicketNumber",
                table: "SupportTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBoqItems_TemplateId",
                table: "TemplateBoqItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateEvaluationCriteria_TemplateId",
                table: "TemplateEvaluationCriteria",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_TemplateId",
                table: "TemplateSections",
                column: "TemplateId");

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
                name: "ActiveDirectoryConfigurations");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowSteps",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "BookletTemplateBlocks");

            migrationBuilder.DropTable(
                name: "CommitteeCompetitions",
                schema: "committees");

            migrationBuilder.DropTable(
                name: "CompetitionCommitteeMembers",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "CompetitionPermissionMatrices",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "FinancialOfferItems",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "FinancialScores",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "InquiryResponses",
                schema: "inquiries");

            migrationBuilder.DropTable(
                name: "PermissionMatrixRules",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "PhaseTransitionHistories",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "TemplateBoqItems");

            migrationBuilder.DropTable(
                name: "TemplateEvaluationCriteria");

            migrationBuilder.DropTable(
                name: "TemplateSections");

            migrationBuilder.DropTable(
                name: "WorkflowStepDefinitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "BookletTemplateSections");

            migrationBuilder.DropTable(
                name: "FinancialEvaluations",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "Inquiries",
                schema: "inquiries");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "CompetitionTemplates");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "BookletTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Committees_TenantId_ScopeType",
                schema: "committees",
                table: "Committees");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "evaluation",
                table: "SupplierOffers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "evaluation",
                table: "SupplierOffers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "evaluation",
                table: "SupplierOffers");

            migrationBuilder.DropColumn(
                name: "IsProtected",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Department",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "ExpectedAwardDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "FiscalYear",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "InquiriesStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "InquiryPeriodDays",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "OffersStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "RequiredAttachmentTypes",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "WorkStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "PhasesString",
                schema: "committees",
                table: "Committees");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                schema: "committees",
                table: "Committees");

            migrationBuilder.DropColumn(
                name: "DeploymentType",
                table: "AiConfigurations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AiConfigurations");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommitteeId",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                column: "ReferenceNumber",
                unique: true);

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
        }
    }
}
