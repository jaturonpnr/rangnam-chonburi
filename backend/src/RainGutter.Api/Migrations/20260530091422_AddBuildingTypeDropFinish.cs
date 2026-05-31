using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingTypeDropFinish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "finish",
                table: "gutter_products");

            migrationBuilder.RenameColumn(
                name: "finish",
                table: "quote_requests",
                newName: "building_type_label_snapshot");

            migrationBuilder.AddColumn<int>(
                name: "building_type_id",
                table: "quote_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_detail",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "building_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    label = table.Column<string>(type: "text", nullable: false),
                    size_inches = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_building_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quote_requests_building_type_id",
                table: "quote_requests",
                column: "building_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_quote_requests_building_types_building_type_id",
                table: "quote_requests",
                column: "building_type_id",
                principalTable: "building_types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_quote_requests_building_types_building_type_id",
                table: "quote_requests");

            migrationBuilder.DropTable(
                name: "building_types");

            migrationBuilder.DropIndex(
                name: "ix_quote_requests_building_type_id",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "building_type_id",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "location_detail",
                table: "leads");

            migrationBuilder.RenameColumn(
                name: "building_type_label_snapshot",
                table: "quote_requests",
                newName: "finish");

            migrationBuilder.AddColumn<string>(
                name: "finish",
                table: "gutter_products",
                type: "text",
                nullable: true);
        }
    }
}
