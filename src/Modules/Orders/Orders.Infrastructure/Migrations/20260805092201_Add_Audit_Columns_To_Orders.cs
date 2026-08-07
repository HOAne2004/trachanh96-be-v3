using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Audit_Columns_To_Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "orders",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "orders",
                table: "Orders");
        }
    }
}
