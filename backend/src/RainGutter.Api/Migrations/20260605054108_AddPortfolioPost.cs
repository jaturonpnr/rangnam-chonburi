using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RainGutter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portfolio_posts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fb_post_url = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    area_name = table.Column<string>(type: "text", nullable: true),
                    approx_lat = table.Column<double>(type: "double precision", nullable: true),
                    approx_lng = table.Column<double>(type: "double precision", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reach = table.Column<int>(type: "integer", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_posts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_posts_fb_post_url",
                table: "portfolio_posts",
                column: "fb_post_url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portfolio_posts");
        }
    }
}
