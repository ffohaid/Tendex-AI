using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "evaluation");

            migrationBuilder.EnsureSchema(
                name: "rfp");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "committees");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AwardRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendedOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendedSupplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinancialScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CombinedScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOfferAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Committees",
                schema: "committees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsPermanent = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActiveFromPhase = table.Column<int>(type: "int", nullable: true),
                    ActiveToPhase = table.Column<int>(type: "int", nullable: true),
                    StatusChangeReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusChangedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Committees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Competitions",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectNameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProjectNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompetitionType = table.Column<int>(type: "int", nullable: false),
                    CreationMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentPhase = table.Column<int>(type: "int", nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "SAR"),
                    SubmissionDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectDurationDays = table.Column<int>(type: "int", nullable: true),
                    TechnicalPassingScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TechnicalWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FinancialWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SourceTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceCompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    LastAutoSavedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentWizardStep = table.Column<int>(type: "int", nullable: false),
                    StatusChangeReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedFromStatus = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationMinutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinutesType = table.Column<int>(type: "int", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationMinutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabledByDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    BucketName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FolderPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ETag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpersonationConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TicketReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpersonationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetTenantName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TicketReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConsentReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ImpersonatedSessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOffers",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SupplierIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfferReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlindCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TechnicalResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TechnicalTotalScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    IsFinancialEnvelopeOpen = table.Column<bool>(type: "bit", nullable: false),
                    FinancialEnvelopeOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinancialEnvelopeOpenedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalEvaluations",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MinimumPassingScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsBlindEvaluationActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TechnicalEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsProvisioned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProvisionedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPersonPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MfaSecretKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoIntegrityAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VideoFileReference = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    VideoFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    VideoFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    VideoDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    ExpectedUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TamperResult = table.Column<int>(type: "int", nullable: false),
                    TamperConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IdentityResult = table.Column<int>(type: "int", nullable: false),
                    IdentityConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    OverallConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    AiProviderUsed = table.Column<int>(type: "int", nullable: true),
                    AiModelUsed = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RawAiResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnalysisStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnalysisCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoIntegrityAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMembers",
                schema: "committees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserFullName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    ActiveFromPhase = table.Column<int>(type: "int", nullable: true),
                    ActiveToPhase = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RemovalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_Committees_CommitteeId",
                        column: x => x.CommitteeId,
                        principalSchema: "committees",
                        principalTable: "Committees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BucketName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalSchema: "rfp",
                        principalTable: "Competitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BoqItems",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedTotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoqItems_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalSchema: "rfp",
                        principalTable: "Competitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCriteria",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WeightPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinimumPassingScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationCriteria_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalSchema: "rfp",
                        principalTable: "Competitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EvaluationCriteria_EvaluationCriteria_ParentCriterionId",
                        column: x => x.ParentCriterionId,
                        principalSchema: "rfp",
                        principalTable: "EvaluationCriteria",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                schema: "rfp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SectionType = table.Column<int>(type: "int", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsFromTemplate = table.Column<bool>(type: "bit", nullable: false),
                    DefaultTextColor = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalSchema: "rfp",
                        principalTable: "Competitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sections_Sections_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalSchema: "rfp",
                        principalTable: "Sections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MinutesSignatories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationMinutesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasSigned = table.Column<bool>(type: "bit", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinutesSignatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MinutesSignatories_EvaluationMinutes_EvaluationMinutesId",
                        column: x => x.EvaluationMinutesId,
                        principalTable: "EvaluationMinutes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AwardRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwardRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    TechnicalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinancialScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CombinedScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOfferAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardRankings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardRankings_AwardRecommendations_AwardRecommendationId",
                        column: x => x.AwardRecommendationId,
                        principalTable: "AwardRecommendations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AwardRankings_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialOfferItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoqItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsArithmeticallyVerified = table.Column<bool>(type: "bit", nullable: false),
                    HasArithmeticError = table.Column<bool>(type: "bit", nullable: false),
                    SupplierSubmittedTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeviationPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeviationLevel = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOfferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialOfferItems_FinancialEvaluations_FinancialEvaluationId",
                        column: x => x.FinancialEvaluationId,
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
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialScores_FinancialEvaluations_FinancialEvaluationId",
                        column: x => x.FinancialEvaluationId,
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
                name: "AiOfferAnalyses",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlindCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    StrengthsAnalysis = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    WeaknessesAnalysis = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    RisksAnalysis = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ComplianceAssessment = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    OverallRecommendation = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    OverallComplianceScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AiModelUsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AiProviderUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnalysisLatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    IsHumanReviewed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiOfferAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiOfferAnalyses_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiOfferAnalyses_TechnicalEvaluations_TechnicalEvaluationId",
                        column: x => x.TechnicalEvaluationId,
                        principalSchema: "evaluation",
                        principalTable: "TechnicalEvaluations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiTechnicalScores",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReferenceCitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTechnicalScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiTechnicalScores_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiTechnicalScores_TechnicalEvaluations_TechnicalEvaluationId",
                        column: x => x.TechnicalEvaluationId,
                        principalSchema: "evaluation",
                        principalTable: "TechnicalEvaluations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnicalScores",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalScores_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalSchema: "evaluation",
                        principalTable: "SupplierOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnicalScores_TechnicalEvaluations_TechnicalEvaluationId",
                        column: x => x.TechnicalEvaluationId,
                        principalSchema: "evaluation",
                        principalTable: "TechnicalEvaluations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    QdrantCollectionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: false, defaultValue: 4096),
                    Temperature = table.Column<double>(type: "float", nullable: false, defaultValue: 0.29999999999999999),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.CheckConstraint("CK_Subscriptions_MaxUsers_Positive", "[MaxUsers] > 0");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantFeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FeatureNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FeatureNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Configuration = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFeatureFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantFeatureFlags_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MfaRecoveryCodes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaRecoveryCodes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResendCount = table.Column<int>(type: "int", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInvitations_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VideoAnalysisFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoIntegrityAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlagCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAnalysisFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoAnalysisFlags_VideoIntegrityAnalyses_VideoIntegrityAnalysisId",
                        column: x => x.VideoIntegrityAnalysisId,
                        principalTable: "VideoIntegrityAnalyses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiCriterionAnalyses",
                schema: "evaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiOfferAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionNameAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SuggestedScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    DetailedJustification = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    OfferCitations = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    BookletRequirementReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ComplianceLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCriterionAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCriterionAnalyses_AiOfferAnalyses_AiOfferAnalysisId",
                        column: x => x.AiOfferAnalysisId,
                        principalSchema: "evaluation",
                        principalTable: "AiOfferAnalyses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_Tenant_Active_Priority",
                table: "AiConfigurations",
                columns: new[] { "TenantId", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_Tenant_Provider_Active",
                table: "AiConfigurations",
                columns: new[] { "TenantId", "Provider", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AiCriterionAnalyses_AiOfferAnalysisId",
                schema: "evaluation",
                table: "AiCriterionAnalyses",
                column: "AiOfferAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCriterionAnalyses_Unique_Analysis_Criterion",
                schema: "evaluation",
                table: "AiCriterionAnalyses",
                columns: new[] { "AiOfferAnalysisId", "EvaluationCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiOfferAnalyses_CompetitionId",
                schema: "evaluation",
                table: "AiOfferAnalyses",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiOfferAnalyses_SupplierOfferId",
                schema: "evaluation",
                table: "AiOfferAnalyses",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_AiOfferAnalyses_TechnicalEvaluationId",
                schema: "evaluation",
                table: "AiOfferAnalyses",
                column: "TechnicalEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiOfferAnalyses_TenantId",
                schema: "evaluation",
                table: "AiOfferAnalyses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiOfferAnalyses_Unique_Evaluation_Offer",
                schema: "evaluation",
                table: "AiOfferAnalyses",
                columns: new[] { "TechnicalEvaluationId", "SupplierOfferId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiTechnicalScores_SupplierOfferId",
                schema: "evaluation",
                table: "AiTechnicalScores",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTechnicalScores_TechnicalEvaluationId",
                schema: "evaluation",
                table: "AiTechnicalScores",
                column: "TechnicalEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTechnicalScores_Unique_Offer_Criterion",
                schema: "evaluation",
                table: "AiTechnicalScores",
                columns: new[] { "TechnicalEvaluationId", "SupplierOfferId", "EvaluationCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CompetitionId",
                schema: "rfp",
                table: "Attachments",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_FileObjectKey",
                schema: "rfp",
                table: "Attachments",
                column: "FileObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActionType_TimestampUtc",
                schema: "audit",
                table: "AuditLogEntries",
                columns: new[] { "ActionType", "TimestampUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_EntityType_EntityId_TimestampUtc",
                schema: "audit",
                table: "AuditLogEntries",
                columns: new[] { "EntityType", "EntityId", "TimestampUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_TenantId_TimestampUtc",
                schema: "audit",
                table: "AuditLogEntries",
                columns: new[] { "TenantId", "TimestampUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_TimestampUtc",
                schema: "audit",
                table: "AuditLogEntries",
                column: "TimestampUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_UserId_TimestampUtc",
                schema: "audit",
                table: "AuditLogEntries",
                columns: new[] { "UserId", "TimestampUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                schema: "audit",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                schema: "audit",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "audit",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardRankings_AwardRecommendationId",
                table: "AwardRankings",
                column: "AwardRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardRankings_SupplierOfferId",
                table: "AwardRankings",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BoqItems_CompetitionId",
                schema: "rfp",
                table: "BoqItems",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_BoqItems_CompetitionId_ItemNumber",
                schema: "rfp",
                table: "BoqItems",
                columns: new[] { "CompetitionId", "ItemNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoqItems_CompetitionId_SortOrder",
                schema: "rfp",
                table: "BoqItems",
                columns: new[] { "CompetitionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_CommitteeId",
                schema: "committees",
                table: "CommitteeMembers",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_CommitteeId_IsActive",
                schema: "committees",
                table: "CommitteeMembers",
                columns: new[] { "CommitteeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_CommitteeId_UserId",
                schema: "committees",
                table: "CommitteeMembers",
                columns: new[] { "CommitteeId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_UserId",
                schema: "committees",
                table: "CommitteeMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_UserId_IsActive",
                schema: "committees",
                table: "CommitteeMembers",
                columns: new[] { "UserId", "IsActive" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId",
                schema: "committees",
                table: "Committees",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId_IsPermanent",
                schema: "committees",
                table: "Committees",
                columns: new[] { "TenantId", "IsPermanent" });

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId_Status",
                schema: "committees",
                table: "Committees",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Committees_TenantId_Type",
                schema: "committees",
                table: "Committees",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_Active",
                schema: "rfp",
                table: "Competitions",
                columns: new[] { "TenantId", "Status" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_CreatedAt",
                schema: "rfp",
                table: "Competitions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_TenantId",
                schema: "rfp",
                table: "Competitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_TenantId_IsDeleted_CreatedAt",
                schema: "rfp",
                table: "Competitions",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_TenantId_Type",
                schema: "rfp",
                table: "Competitions",
                columns: new[] { "TenantId", "CompetitionType" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_CompetitionId",
                schema: "rfp",
                table: "EvaluationCriteria",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_CompetitionId_SortOrder",
                schema: "rfp",
                table: "EvaluationCriteria",
                columns: new[] { "CompetitionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_ParentCriterionId",
                schema: "rfp",
                table: "EvaluationCriteria",
                column: "ParentCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDefinitions_Category",
                table: "FeatureDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDefinitions_FeatureKey",
                table: "FeatureDefinitions",
                column: "FeatureKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDefinitions_IsActive",
                table: "FeatureDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_Category",
                table: "FileAttachments",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_IsDeleted_Active",
                table: "FileAttachments",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_ObjectKey",
                table: "FileAttachments",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_TenantId",
                table: "FileAttachments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_TenantId_IsDeleted",
                table: "FileAttachments",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_FinancialEvaluationId",
                table: "FinancialOfferItems",
                column: "FinancialEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_SupplierOfferId",
                table: "FinancialOfferItems",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_FinancialEvaluationId",
                table: "FinancialScores",
                column: "FinancialEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_SupplierOfferId",
                table: "FinancialScores",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationConsents_RequestedAtUtc",
                table: "ImpersonationConsents",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationConsents_RequestedByUserId",
                table: "ImpersonationConsents",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationConsents_Status",
                table: "ImpersonationConsents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationConsents_TargetUserId",
                table: "ImpersonationConsents",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_AdminUserId",
                table: "ImpersonationSessions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_StartedAtUtc",
                table: "ImpersonationSessions",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_Status",
                table: "ImpersonationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_TargetTenantId",
                table: "ImpersonationSessions",
                column: "TargetTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_TargetUserId",
                table: "ImpersonationSessions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_User_Used",
                schema: "identity",
                table: "MfaRecoveryCodes",
                columns: new[] { "UserId", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_MinutesSignatories_EvaluationMinutesId",
                table: "MinutesSignatories",
                column: "EvaluationMinutesId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "identity",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "identity",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_User_Revoked",
                schema: "identity",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_Role_Permission",
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Tenant_NormalizedName",
                schema: "identity",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CompetitionId",
                schema: "rfp",
                table: "Sections",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CompetitionId_SortOrder",
                schema: "rfp",
                table: "Sections",
                columns: new[] { "CompetitionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Sections_ParentSectionId",
                schema: "rfp",
                table: "Sections",
                column: "ParentSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Tenant_Active",
                table: "Subscriptions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_BlindCode",
                schema: "evaluation",
                table: "SupplierOffers",
                column: "BlindCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_CompetitionId",
                schema: "evaluation",
                table: "SupplierOffers",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_CompetitionId_SupplierIdentifier",
                schema: "evaluation",
                table: "SupplierOffers",
                columns: new[] { "CompetitionId", "SupplierIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_CompetitionId_TechnicalResult",
                schema: "evaluation",
                table: "SupplierOffers",
                columns: new[] { "CompetitionId", "TechnicalResult" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_TenantId",
                schema: "evaluation",
                table: "SupplierOffers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalEvaluations_CommitteeId",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalEvaluations_CompetitionId",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                column: "CompetitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalEvaluations_Status",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalEvaluations_TenantId",
                schema: "evaluation",
                table: "TechnicalEvaluations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalScores_EvaluationCriterionId",
                schema: "evaluation",
                table: "TechnicalScores",
                column: "EvaluationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalScores_SupplierOfferId",
                schema: "evaluation",
                table: "TechnicalScores",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalScores_TechnicalEvaluationId",
                schema: "evaluation",
                table: "TechnicalScores",
                column: "TechnicalEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalScores_Unique_Evaluator_Criterion_Offer",
                schema: "evaluation",
                table: "TechnicalScores",
                columns: new[] { "TechnicalEvaluationId", "SupplierOfferId", "EvaluationCriterionId", "EvaluatorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatureFlags_Tenant_Enabled",
                table: "TenantFeatureFlags",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatureFlags_Tenant_FeatureKey",
                table: "TenantFeatureFlags",
                columns: new[] { "TenantId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants",
                column: "DatabaseName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                table: "Tenants",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Status",
                table: "Tenants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionExpiresAt",
                table: "Tenants",
                column: "SubscriptionExpiresAt",
                filter: "[SubscriptionExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_ExpiresAt",
                table: "UserInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_InvitedByUserId",
                table: "UserInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_NormalizedEmail_TenantId",
                table: "UserInvitations",
                columns: new[] { "NormalizedEmail", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_RoleId",
                table: "UserInvitations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_TenantId_Status",
                table: "UserInvitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_Token",
                table: "UserInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_User_Role",
                schema: "identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                schema: "identity",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUserName",
                schema: "identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_Email",
                schema: "identity",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                schema: "identity",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAnalysisFlags_FlagCode",
                table: "VideoAnalysisFlags",
                column: "FlagCode");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAnalysisFlags_VideoIntegrityAnalysisId",
                table: "VideoAnalysisFlags",
                column: "VideoIntegrityAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_CompetitionId",
                table: "VideoIntegrityAnalyses",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_Status",
                table: "VideoIntegrityAnalyses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_SupplierOfferId",
                table: "VideoIntegrityAnalyses",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_TenantId",
                table: "VideoIntegrityAnalyses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_TenantId_Status",
                table: "VideoIntegrityAnalyses",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoIntegrityAnalyses_VideoFileReference",
                table: "VideoIntegrityAnalyses",
                column: "VideoFileReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConfigurations");

            migrationBuilder.DropTable(
                name: "AiCriterionAnalyses",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "AiTechnicalScores",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "AuditLogEntries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "AwardRankings");

            migrationBuilder.DropTable(
                name: "BoqItems",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "CommitteeMembers",
                schema: "committees");

            migrationBuilder.DropTable(
                name: "EvaluationCriteria",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "FeatureDefinitions");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "FinancialOfferItems");

            migrationBuilder.DropTable(
                name: "FinancialScores");

            migrationBuilder.DropTable(
                name: "ImpersonationConsents");

            migrationBuilder.DropTable(
                name: "ImpersonationSessions");

            migrationBuilder.DropTable(
                name: "MfaRecoveryCodes",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "MinutesSignatories");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Sections",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TechnicalScores",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "TenantFeatureFlags");

            migrationBuilder.DropTable(
                name: "UserInvitations");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "VideoAnalysisFlags");

            migrationBuilder.DropTable(
                name: "AiOfferAnalyses",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "AwardRecommendations");

            migrationBuilder.DropTable(
                name: "Committees",
                schema: "committees");

            migrationBuilder.DropTable(
                name: "FinancialEvaluations");

            migrationBuilder.DropTable(
                name: "EvaluationMinutes");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Competitions",
                schema: "rfp");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "VideoIntegrityAnalyses");

            migrationBuilder.DropTable(
                name: "SupplierOffers",
                schema: "evaluation");

            migrationBuilder.DropTable(
                name: "TechnicalEvaluations",
                schema: "evaluation");
        }
    }
}
