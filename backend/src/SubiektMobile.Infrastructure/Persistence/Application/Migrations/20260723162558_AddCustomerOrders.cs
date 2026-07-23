using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_warehouse_order_items_WarehouseOrderId_ProductId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.AddColumn<string>(
                name: "CustomerDeliveryNoteNumber",
                schema: "app",
                table: "warehouse_orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerOrderId",
                schema: "app",
                table: "warehouse_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerOrderItemId",
                schema: "app",
                table: "warehouse_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPackagingCode",
                schema: "app",
                table: "warehouse_order_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPartNumber",
                schema: "app",
                table: "warehouse_order_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultPackagingTypeId",
                schema: "app",
                table: "warehouse_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineeringChange",
                schema: "app",
                table: "warehouse_order_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_orders",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerSiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerOrderNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DeliveryNoteNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RequestedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_orders_customer_sites_CustomerSiteId",
                        column: x => x.CustomerSiteId,
                        principalSchema: "app",
                        principalTable: "customer_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_orders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "app",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_order_items",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPartNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedCustomerPartNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_order_items_customer_orders_CustomerOrderId",
                        column: x => x.CustomerOrderId,
                        principalSchema: "app",
                        principalTable: "customer_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_orders_CustomerOrderId",
                schema: "app",
                table: "warehouse_orders",
                column: "CustomerOrderId",
                unique: true,
                filter: "\"CustomerOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_order_items_CustomerOrderItemId",
                schema: "app",
                table: "warehouse_order_items",
                column: "CustomerOrderItemId",
                unique: true,
                filter: "\"CustomerOrderItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_order_items_WarehouseOrderId_ProductId",
                schema: "app",
                table: "warehouse_order_items",
                columns: new[] { "WarehouseOrderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_order_items_CustomerOrderId_NormalizedCustomerPart~",
                schema: "app",
                table: "customer_order_items",
                columns: new[] { "CustomerOrderId", "NormalizedCustomerPartNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_CustomerId_RequestedDeliveryDate",
                schema: "app",
                table: "customer_orders",
                columns: new[] { "CustomerId", "RequestedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_CustomerSiteId_RequestedDeliveryDate",
                schema: "app",
                table: "customer_orders",
                columns: new[] { "CustomerSiteId", "RequestedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_Status_UpdatedAtUtc",
                schema: "app",
                table: "customer_orders",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_order_items_customer_order_items_CustomerOrderIte~",
                schema: "app",
                table: "warehouse_order_items",
                column: "CustomerOrderItemId",
                principalSchema: "app",
                principalTable: "customer_order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_orders_customer_orders_CustomerOrderId",
                schema: "app",
                table: "warehouse_orders",
                column: "CustomerOrderId",
                principalSchema: "app",
                principalTable: "customer_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_order_items_customer_order_items_CustomerOrderIte~",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_orders_customer_orders_CustomerOrderId",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropTable(
                name: "customer_order_items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "customer_orders",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_orders_CustomerOrderId",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_order_items_CustomerOrderItemId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_order_items_WarehouseOrderId_ProductId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "CustomerDeliveryNoteNumber",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropColumn(
                name: "CustomerOrderId",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropColumn(
                name: "CustomerOrderItemId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "CustomerPackagingCode",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "CustomerPartNumber",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "DefaultPackagingTypeId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "EngineeringChange",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_order_items_WarehouseOrderId_ProductId",
                schema: "app",
                table: "warehouse_order_items",
                columns: new[] { "WarehouseOrderId", "ProductId" },
                unique: true);
        }
    }
}
