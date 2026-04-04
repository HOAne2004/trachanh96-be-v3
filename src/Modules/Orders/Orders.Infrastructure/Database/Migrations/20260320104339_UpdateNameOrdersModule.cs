using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNameOrdersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Orders",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "OrderItems",
                newSchema: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                schema: "orders",
                newName: "OrderItems");
        }
    }
}
