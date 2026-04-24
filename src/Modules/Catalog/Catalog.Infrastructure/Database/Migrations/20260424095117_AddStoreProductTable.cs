using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreProducts",
                schema: "catalog",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    PriceOverride = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProducts", x => new { x.StoreId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_StoreProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductToppings_ToppingId",
                schema: "catalog",
                table: "ProductToppings",
                column: "ToppingId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_ProductId",
                schema: "catalog",
                table: "StoreProducts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductToppings_Toppings_ToppingId",
                schema: "catalog",
                table: "ProductToppings",
                column: "ToppingId",
                principalSchema: "catalog",
                principalTable: "Toppings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductToppings_Toppings_ToppingId",
                schema: "catalog",
                table: "ProductToppings");

            migrationBuilder.DropTable(
                name: "StoreProducts",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_ProductToppings_ToppingId",
                schema: "catalog",
                table: "ProductToppings");
        }
    }
}
