using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint9UpdatesMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Competitions_TenantId_IsDeleted_CreatedAt",
                schema: "rfp",
                table: "Competitions",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitions_TenantId_IsDeleted_CreatedAt",
                schema: "rfp",
                table: "Competitions");
        }
    }
}
