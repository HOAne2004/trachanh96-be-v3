using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrdersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrderType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeliveryDetails_RecipientName = table.Column<string>(type: "text", nullable: true),
                    DeliveryDetails_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    DeliveryDetails_Address = table.Column<string>(type: "text", nullable: true),
                    DeliveryDetails_PickupTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryDetails_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    DeliveryDetails_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    DeliveryDetails_DistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    DeliveryDetails_ProviderName = table.Column<string>(type: "text", nullable: true),
                    DeliveryDetails_TrackingId = table.Column<string>(type: "text", nullable: true),
                    TableId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ShippingFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingFeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FinalTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalTotalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AppliedVoucherCode = table.Column<string>(type: "text", nullable: true),
                    VoucherDiscountType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VoucherDiscountValue = table.Column<decimal>(type: "numeric", nullable: true),
                    VoucherMaxDiscount = table.Column<decimal>(type: "numeric", nullable: true),
                    VoucherMinOrderValue = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SizeName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IceLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SugarLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Toppings = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "orders",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_CreatedAt",
                schema: "orders",
                table: "Orders",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                schema: "orders",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_OrderStatus_CreatedAt",
                schema: "orders",
                table: "Orders",
                columns: new[] { "StoreId", "OrderStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                schema: "orders",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                schema: "orders",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "orders");
        }
    }
}
