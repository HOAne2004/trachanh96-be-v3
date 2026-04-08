using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryInfoToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeId",
                schema: "orders",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "AppliedVoucherCode",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedOutAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNotes",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDetails_Address",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryDetails_DistanceKm",
                schema: "orders",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryDetails_Latitude",
                schema: "orders",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryDetails_Longitude",
                schema: "orders",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDetails_PhoneNumber",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDetails_PickupTime",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDetails_ProviderName",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDetails_RecipientName",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDetails_TrackingId",
                schema: "orders",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                schema: "orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFeeAmount",
                schema: "orders",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFeeCurrency",
                schema: "orders",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TableId",
                schema: "orders",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherDiscountType",
                schema: "orders",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherDiscountValue",
                schema: "orders",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherMaxDiscount",
                schema: "orders",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherMinOrderValue",
                schema: "orders",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SizeName",
                schema: "orders",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "IceLevel",
                schema: "orders",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SugarLevel",
                schema: "orders",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedVoucherCode",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckedOutAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerNotes",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_Address",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_DistanceKm",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_Latitude",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_Longitude",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_PhoneNumber",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_PickupTime",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_ProviderName",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_RecipientName",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDetails_TrackingId",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFeeAmount",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFeeCurrency",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableId",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountType",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountValue",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherMaxDiscount",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherMinOrderValue",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IceLevel",
                schema: "orders",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SugarLevel",
                schema: "orders",
                table: "OrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "SizeName",
                schema: "orders",
                table: "OrderItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<Guid>(
                name: "SizeId",
                schema: "orders",
                table: "OrderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
