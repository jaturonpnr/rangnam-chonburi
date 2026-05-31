using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gutter_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    material = table.Column<string>(type: "text", nullable: false),
                    size_inches = table.Column<int>(type: "integer", nullable: false),
                    finish = table.Column<string>(type: "text", nullable: true),
                    price_per_meter = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gutter_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    minimum_meters = table.Column<decimal>(type: "numeric", nullable: false),
                    downspout_price_per_point = table.Column<decimal>(type: "numeric", nullable: false),
                    height_surcharge_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    removal_price_per_meter = table.Column<decimal>(type: "numeric", nullable: false),
                    survey_fee = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_zones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    travel_surcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shop_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    line_oa_link = table.Column<string>(type: "text", nullable: false),
                    quote_validity_days = table.Column<int>(type: "integer", nullable: false),
                    quote_footer_note = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shop_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    service_zone_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.ForeignKey(
                        name: "fk_leads_service_zones_service_zone_id",
                        column: x => x.service_zone_id,
                        principalTable: "service_zones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quote_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quote_number = table.Column<string>(type: "text", nullable: false),
                    lead_id = table.Column<int>(type: "integer", nullable: false),
                    material = table.Column<string>(type: "text", nullable: false),
                    size_inches = table.Column<int>(type: "integer", nullable: false),
                    finish = table.Column<string>(type: "text", nullable: true),
                    length_meters = table.Column<decimal>(type: "numeric", nullable: false),
                    downspout_count = table.Column<int>(type: "integer", nullable: false),
                    floors = table.Column<int>(type: "integer", nullable: false),
                    remove_old = table.Column<bool>(type: "boolean", nullable: false),
                    estimated_total = table.Column<decimal>(type: "numeric", nullable: false),
                    breakdown_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_requests_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_leads_service_zone_id",
                table: "leads",
                column: "service_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_requests_lead_id",
                table: "quote_requests",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_requests_quote_number",
                table: "quote_requests",
                column: "quote_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "gutter_products");

            migrationBuilder.DropTable(
                name: "pricing_configs");

            migrationBuilder.DropTable(
                name: "quote_requests");

            migrationBuilder.DropTable(
                name: "shop_profiles");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "service_zones");
        }
    }
}
