using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update_Product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TotalRating",
                schema: "catalog",
                table: "Products",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSold",
                schema: "catalog",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalRating",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TotalSold",
                schema: "catalog",
                table: "Products");
        }
    }
}
