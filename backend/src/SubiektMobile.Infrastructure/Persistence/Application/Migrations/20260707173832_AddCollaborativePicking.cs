using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborativePicking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PackedAtUtc",
                schema: "app",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackedById",
                schema: "app",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackedByKind",
                schema: "app",
                table: "order_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackedByName",
                schema: "app",
                table: "order_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackedQuantity",
                schema: "app",
                table: "order_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservedAtUtc",
                schema: "app",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReservedById",
                schema: "app",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservedByKind",
                schema: "app",
                table: "order_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservedByName",
                schema: "app",
                table: "order_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "app",
                table: "order_items",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "order_picking_events",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PackedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ActorKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_picking_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_picking_events_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "app",
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_picking_events_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "app",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId_Status",
                schema: "app",
                table: "order_items",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_order_picking_events_OperationId",
                schema: "app",
                table: "order_picking_events",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_picking_events_OrderId_OccurredAtUtc",
                schema: "app",
                table: "order_picking_events",
                columns: new[] { "OrderId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_order_picking_events_OrderItemId",
                schema: "app",
                table: "order_picking_events",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_picking_events",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_order_items_OrderId_Status",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PackedAtUtc",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PackedById",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PackedByKind",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PackedByName",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PackedQuantity",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ReservedAtUtc",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ReservedById",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ReservedByKind",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ReservedByName",
                schema: "app",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "app",
                table: "order_items");
        }
    }
}
