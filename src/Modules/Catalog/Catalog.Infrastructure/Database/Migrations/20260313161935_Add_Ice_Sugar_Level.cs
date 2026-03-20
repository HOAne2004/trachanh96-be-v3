using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_Ice_Sugar_Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short[]>(
                name: "AllowedIceLevels",
                schema: "catalog",
                table: "Products",
                type: "smallint[]",
                nullable: false,
                defaultValue: new short[0]);

            migrationBuilder.AddColumn<short[]>(
                name: "AllowedSugarLevels",
                schema: "catalog",
                table: "Products",
                type: "smallint[]",
                nullable: false,
                defaultValue: new short[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedIceLevels",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AllowedSugarLevels",
                schema: "catalog",
                table: "Products");
        }
    }
}
