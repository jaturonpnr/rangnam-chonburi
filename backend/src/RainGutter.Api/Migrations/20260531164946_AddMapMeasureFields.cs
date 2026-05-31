using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMapMeasureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "map_center_lat",
                table: "quote_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "map_center_lng",
                table: "quote_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "map_zoom",
                table: "quote_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "measure_source",
                table: "quote_requests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "measured_geo_json",
                table: "quote_requests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "measured_length_meters",
                table: "quote_requests",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "map_center_lat",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "map_center_lng",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "map_zoom",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "measure_source",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "measured_geo_json",
                table: "quote_requests");

            migrationBuilder.DropColumn(
                name: "measured_length_meters",
                table: "quote_requests");
        }
    }
}
