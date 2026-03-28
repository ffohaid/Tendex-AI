using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialMasterPlatform : Migration
{
    private static readonly string[] AiConfigIndexColumns = ["TenantId", "Provider", "IsActive"];
    private static readonly string[] SubscriptionIndexColumns = ["TenantId", "IsActive"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ConnectionString = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LogoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                PrimaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                SecondaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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

        migrationBuilder.CreateIndex(
            name: "IX_AiConfigurations_Tenant_Provider_Active",
            table: "AiConfigurations",
            columns: AiConfigIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_Tenant_Active",
            table: "Subscriptions",
            columns: SubscriptionIndexColumns);

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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AiConfigurations");

        migrationBuilder.DropTable(
            name: "Subscriptions");

        migrationBuilder.DropTable(
            name: "Tenants");
    }
}
