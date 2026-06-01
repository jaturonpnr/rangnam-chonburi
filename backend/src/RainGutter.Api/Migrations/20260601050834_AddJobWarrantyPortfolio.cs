using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWarrantyPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quote_request_id = table.Column<int>(type: "integer", nullable: false),
                    warranty_number = table.Column<string>(type: "text", nullable: false),
                    public_token = table.Column<string>(type: "text", nullable: false),
                    installed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    warranty_months = table.Column<int>(type: "integer", nullable: false),
                    material = table.Column<string>(type: "text", nullable: false),
                    size_inches = table.Column<int>(type: "integer", nullable: false),
                    length_meters = table.Column<decimal>(type: "numeric", nullable: false),
                    downspout_count = table.Column<int>(type: "integer", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: true),
                    lng = table.Column<double>(type: "double precision", nullable: true),
                    approx_lat = table.Column<double>(type: "double precision", nullable: true),
                    approx_lng = table.Column<double>(type: "double precision", nullable: true),
                    area_name = table.Column<string>(type: "text", nullable: true),
                    show_in_portfolio = table.Column<bool>(type: "boolean", nullable: false),
                    photo_consent = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_jobs_quote_requests_quote_request_id",
                        column: x => x.quote_request_id,
                        principalTable: "quote_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_photos_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    contact_phone = table.Column<string>(type: "text", nullable: false),
                    customer_note = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_requests_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_photos_job_id",
                table: "job_photos",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_public_token",
                table: "jobs",
                column: "public_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_quote_request_id",
                table: "jobs",
                column: "quote_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_warranty_number",
                table: "jobs",
                column: "warranty_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_job_id",
                table: "service_requests",
                column: "job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_photos");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "jobs");
        }
    }
}
