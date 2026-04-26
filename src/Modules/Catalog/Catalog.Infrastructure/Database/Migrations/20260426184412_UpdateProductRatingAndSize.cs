using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductRatingAndSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceOverride_Currency",
                schema: "catalog",
                table: "ProductSizes",
                newName: "PPriceModifier_Currency");

            migrationBuilder.RenameColumn(
                name: "PriceOverride_Amount",
                schema: "catalog",
                table: "ProductSizes",
                newName: "PriceModifier_Amount");

            migrationBuilder.RenameColumn(
                name: "TotalRating",
                schema: "catalog",
                table: "Products",
                newName: "TotalRatingScore");

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                schema: "catalog",
                table: "StoreProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoldCount",
                schema: "catalog",
                table: "StoreProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalRatingScore",
                schema: "catalog",
                table: "StoreProducts",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
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
                name: "RatingCount",
                schema: "catalog",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "SoldCount",
                schema: "catalog",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "TotalRatingScore",
                schema: "catalog",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                schema: "catalog",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "PriceModifier_Amount",
                schema: "catalog",
                table: "ProductSizes",
                newName: "PriceOverride_Amount");

            migrationBuilder.RenameColumn(
                name: "PPriceModifier_Currency",
                schema: "catalog",
                table: "ProductSizes",
                newName: "PriceOverride_Currency");

            migrationBuilder.RenameColumn(
                name: "TotalRatingScore",
                schema: "catalog",
                table: "Products",
                newName: "TotalRating");
        }
    }
}
