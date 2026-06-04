using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNullFilteredJobIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_jobs_public_token",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_jobs_warranty_number",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_public_token",
                table: "jobs",
                column: "public_token",
                unique: true,
                filter: "public_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_warranty_number",
                table: "jobs",
                column: "warranty_number",
                unique: true,
                filter: "warranty_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_jobs_public_token",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_jobs_warranty_number",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_public_token",
                table: "jobs",
                column: "public_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_warranty_number",
                table: "jobs",
                column: "warranty_number",
                unique: true);
        }
    }
}
