using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant
{
    public partial class AddBasicInfoFieldsToCompetitions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

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

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "rfp",
                table: "Competitions",
                type: "nvarchar(max)",
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

            migrationBuilder.AddColumn<int>(
                name: "InquiryPeriodDays",
                schema: "rfp",
                table: "Competitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InquiriesStartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OffersStartDate",
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

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                column: "ReferenceNumber",
                unique: true,
                filter: "[ReferenceNumber] IS NOT NULL AND [IsDeleted] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "Department",
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
                name: "InquiryPeriodDays",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "InquiriesStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "OffersStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "WorkStartDate",
                schema: "rfp",
                table: "Competitions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
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

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                schema: "rfp",
                table: "Competitions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ReferenceNumber",
                schema: "rfp",
                table: "Competitions",
                column: "ReferenceNumber",
                unique: true);
        }
    }
}
