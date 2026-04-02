using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddInquiryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialScores_FinancialEvaluationId",
                table: "FinancialScores");

            migrationBuilder.DropIndex(
                name: "IX_FinancialOfferItems_FinancialEvaluationId",
                table: "FinancialOfferItems");

            migrationBuilder.EnsureSchema(
                name: "inquiries");

            migrationBuilder.RenameTable(
                name: "FinancialScores",
                newName: "FinancialScores",
                newSchema: "evaluation");

            migrationBuilder.RenameTable(
                name: "FinancialOfferItems",
                newName: "FinancialOfferItems",
                newSchema: "evaluation");

            migrationBuilder.RenameTable(
                name: "FinancialEvaluations",
                newName: "FinancialEvaluations",
                newSchema: "evaluation");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                schema: "evaluation",
                table: "FinancialScores",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "evaluation",
                table: "FinancialScores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                schema: "evaluation",
                table: "FinancialScores",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "evaluation",
                table: "FinancialScores",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EvaluatorUserId",
                schema: "evaluation",
                table: "FinancialScores",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "evaluation",
                table: "FinancialScores",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsArithmeticallyVerified",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "HasArithmeticError",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<decimal>(
                name: "DeviationPercentage",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviationLevel",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "evaluation",
                table: "FinancialOfferItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "evaluation",
                table: "FinancialEvaluations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                schema: "evaluation",
                table: "FinancialEvaluations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "evaluation",
                table: "FinancialEvaluations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "evaluation",
                table: "FinancialEvaluations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                schema: "evaluation",
                table: "FinancialEvaluations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_Eval_Offer_Evaluator",
                schema: "evaluation",
                table: "FinancialScores",
                columns: new[] { "FinancialEvaluationId", "SupplierOfferId", "EvaluatorUserId" },
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InquiryResponses",
                schema: "inquiries");

            migrationBuilder.DropTable(
                name: "Inquiries",
                schema: "inquiries");

            migrationBuilder.DropIndex(
                name: "IX_FinancialScores_Eval_Offer_Evaluator",
                schema: "evaluation",
                table: "FinancialScores");

            migrationBuilder.DropIndex(
                name: "IX_FinancialOfferItems_BoqItemId",
                schema: "evaluation",
                table: "FinancialOfferItems");

            migrationBuilder.DropIndex(
                name: "IX_FinancialOfferItems_Eval_Offer_Boq",
                schema: "evaluation",
                table: "FinancialOfferItems");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvaluations_CommitteeId",
                schema: "evaluation",
                table: "FinancialEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvaluations_CompetitionId",
                schema: "evaluation",
                table: "FinancialEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvaluations_Status",
                schema: "evaluation",
                table: "FinancialEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvaluations_TechnicalEvaluationId",
                schema: "evaluation",
                table: "FinancialEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvaluations_TenantId",
                schema: "evaluation",
                table: "FinancialEvaluations");

            migrationBuilder.RenameTable(
                name: "FinancialScores",
                schema: "evaluation",
                newName: "FinancialScores");

            migrationBuilder.RenameTable(
                name: "FinancialOfferItems",
                schema: "evaluation",
                newName: "FinancialOfferItems");

            migrationBuilder.RenameTable(
                name: "FinancialEvaluations",
                schema: "evaluation",
                newName: "FinancialEvaluations");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "FinancialScores",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "FinancialScores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "FinancialScores",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "FinancialScores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EvaluatorUserId",
                table: "FinancialScores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FinancialScores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "FinancialOfferItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "FinancialOfferItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsArithmeticallyVerified",
                table: "FinancialOfferItems",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "HasArithmeticError",
                table: "FinancialOfferItems",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "DeviationPercentage",
                table: "FinancialOfferItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeviationLevel",
                table: "FinancialOfferItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FinancialOfferItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FinancialEvaluations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "FinancialEvaluations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "FinancialEvaluations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "FinancialEvaluations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "FinancialEvaluations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialScores_FinancialEvaluationId",
                table: "FinancialScores",
                column: "FinancialEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOfferItems_FinancialEvaluationId",
                table: "FinancialOfferItems",
                column: "FinancialEvaluationId");
        }
    }
}
