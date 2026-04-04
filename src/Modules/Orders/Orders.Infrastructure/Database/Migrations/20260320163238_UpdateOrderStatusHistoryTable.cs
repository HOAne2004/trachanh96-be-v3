using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistory_Orders_OrderId",
                table: "OrderStatusHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderStatusHistory",
                table: "OrderStatusHistory");

            migrationBuilder.RenameTable(
                name: "OrderStatusHistory",
                newName: "OrderStatusHistories",
                newSchema: "orders");

            migrationBuilder.RenameIndex(
                name: "IX_OrderStatusHistory_OrderId",
                schema: "orders",
                table: "OrderStatusHistories",
                newName: "IX_OrderStatusHistories_OrderId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                schema: "orders",
                table: "OrderStatusHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderStatusHistories",
                schema: "orders",
                table: "OrderStatusHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistories_Orders_OrderId",
                schema: "orders",
                table: "OrderStatusHistories",
                column: "OrderId",
                principalSchema: "orders",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistories_Orders_OrderId",
                schema: "orders",
                table: "OrderStatusHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderStatusHistories",
                schema: "orders",
                table: "OrderStatusHistories");

            migrationBuilder.RenameTable(
                name: "OrderStatusHistories",
                schema: "orders",
                newName: "OrderStatusHistory");

            migrationBuilder.RenameIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistory",
                newName: "IX_OrderStatusHistory_OrderId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "OrderStatusHistory",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderStatusHistory",
                table: "OrderStatusHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistory_Orders_OrderId",
                table: "OrderStatusHistory",
                column: "OrderId",
                principalSchema: "orders",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
