using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPalletization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pallets",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmptyPalletWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GoodsWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ClosedByKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedByName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pallets_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "app",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pallet_items",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LineWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pallet_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pallet_items_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "app",
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pallet_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "app",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pallet_items_pallets_PalletId",
                        column: x => x.PalletId,
                        principalSchema: "app",
                        principalTable: "pallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pallet_items_OrderId",
                schema: "app",
                table: "pallet_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_pallet_items_OrderItemId",
                schema: "app",
                table: "pallet_items",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_pallet_items_PalletId_OrderItemId",
                schema: "app",
                table: "pallet_items",
                columns: new[] { "PalletId", "OrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pallets_number",
                schema: "app",
                table: "pallets",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pallets_operation_id",
                schema: "app",
                table: "pallets",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pallets_OrderId_ClosedAtUtc",
                schema: "app",
                table: "pallets",
                columns: new[] { "OrderId", "ClosedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pallet_items",
                schema: "app");

            migrationBuilder.DropTable(
                name: "pallets",
                schema: "app");
        }
    }
}
