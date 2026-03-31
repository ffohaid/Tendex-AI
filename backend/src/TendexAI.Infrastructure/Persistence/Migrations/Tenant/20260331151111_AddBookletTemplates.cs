using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBookletTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookletTemplateBlocks");

            migrationBuilder.DropTable(
                name: "TemplateBoqItems");

            migrationBuilder.DropTable(
                name: "TemplateEvaluationCriteria");

            migrationBuilder.DropTable(
                name: "TemplateSections");

            migrationBuilder.DropTable(
                name: "BookletTemplateSections");

            migrationBuilder.DropTable(
                name: "CompetitionTemplates");

            migrationBuilder.DropTable(
                name: "BookletTemplates");
        }
    }
}
