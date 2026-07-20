using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations;

/// <inheritdoc />
public partial class RenameOrdersToWarehouseOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_pallet_items_order_items_OrderItemId",
            schema: "app",
            table: "pallet_items");

        migrationBuilder.DropForeignKey(
            name: "FK_pallet_items_orders_OrderId",
            schema: "app",
            table: "pallet_items");

        migrationBuilder.DropForeignKey(
            name: "FK_pallets_orders_OrderId",
            schema: "app",
            table: "pallets");

        migrationBuilder.RenameTable(name: "orders", schema: "app", newName: "warehouse_orders");
        migrationBuilder.RenameTable(name: "order_items", schema: "app", newName: "warehouse_order_items");
        migrationBuilder.RenameTable(name: "order_assignees", schema: "app", newName: "warehouse_order_assignees");
        migrationBuilder.RenameTable(name: "order_picking_events", schema: "app", newName: "warehouse_order_picking_events");

        migrationBuilder.RenameColumn(
            name: "OrderId", schema: "app", table: "warehouse_order_items", newName: "WarehouseOrderId");
        migrationBuilder.RenameColumn(
            name: "OrderId", schema: "app", table: "warehouse_order_assignees", newName: "WarehouseOrderId");
        migrationBuilder.RenameColumn(
            name: "OrderId", schema: "app", table: "warehouse_order_picking_events", newName: "WarehouseOrderId");
        migrationBuilder.RenameColumn(
            name: "OrderItemId", schema: "app", table: "warehouse_order_picking_events", newName: "WarehouseOrderItemId");
        migrationBuilder.RenameColumn(
            name: "OrderId", schema: "app", table: "pallets", newName: "WarehouseOrderId");
        migrationBuilder.RenameColumn(
            name: "OrderId", schema: "app", table: "pallet_items", newName: "WarehouseOrderId");
        migrationBuilder.RenameColumn(
            name: "OrderItemId", schema: "app", table: "pallet_items", newName: "WarehouseOrderItemId");

        migrationBuilder.RenameIndex(
            name: "IX_orders_Number", schema: "app", table: "warehouse_orders", newName: "IX_warehouse_orders_Number");
        migrationBuilder.RenameIndex(
            name: "IX_orders_Status_UpdatedAtUtc", schema: "app", table: "warehouse_orders", newName: "IX_warehouse_orders_Status_UpdatedAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_order_items_OrderId_ProductId", schema: "app", table: "warehouse_order_items", newName: "IX_warehouse_order_items_WarehouseOrderId_ProductId");
        migrationBuilder.RenameIndex(
            name: "IX_order_items_OrderId_Status", schema: "app", table: "warehouse_order_items", newName: "IX_warehouse_order_items_WarehouseOrderId_Status");
        migrationBuilder.RenameIndex(
            name: "IX_order_assignees_EmployeeId", schema: "app", table: "warehouse_order_assignees", newName: "IX_warehouse_order_assignees_EmployeeId");
        migrationBuilder.RenameIndex(
            name: "IX_order_assignees_OrganizationId", schema: "app", table: "warehouse_order_assignees", newName: "IX_warehouse_order_assignees_OrganizationId");
        migrationBuilder.RenameIndex(
            name: "IX_order_assignees_OrderId_EmployeeId", schema: "app", table: "warehouse_order_assignees", newName: "IX_warehouse_order_assignees_WarehouseOrderId_EmployeeId");
        migrationBuilder.RenameIndex(
            name: "IX_order_picking_events_OperationId", schema: "app", table: "warehouse_order_picking_events", newName: "IX_warehouse_order_picking_events_OperationId");
        migrationBuilder.RenameIndex(
            name: "IX_order_picking_events_OrderItemId", schema: "app", table: "warehouse_order_picking_events", newName: "IX_warehouse_order_picking_events_WarehouseOrderItemId");
        migrationBuilder.RenameIndex(
            name: "IX_order_picking_events_OrderId_OccurredAtUtc", schema: "app", table: "warehouse_order_picking_events", newName: "IX_warehouse_order_picking_events_WarehouseOrderId_OccurredAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_pallets_OrderId_ClosedAtUtc", schema: "app", table: "pallets", newName: "IX_pallets_WarehouseOrderId_ClosedAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_OrderId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_WarehouseOrderId");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_OrderItemId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_WarehouseOrderItemId");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_PalletId_OrderItemId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_PalletId_WarehouseOrderItemId");

        migrationBuilder.Sql("ALTER TABLE app.warehouse_orders RENAME CONSTRAINT \"PK_orders\" TO \"PK_warehouse_orders\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_items RENAME CONSTRAINT \"PK_order_items\" TO \"PK_warehouse_order_items\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"PK_order_assignees\" TO \"PK_warehouse_order_assignees\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"PK_order_picking_events\" TO \"PK_warehouse_order_picking_events\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_items RENAME CONSTRAINT \"FK_order_items_orders_OrderId\" TO \"FK_warehouse_order_items_warehouse_orders_WarehouseOrderId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_order_assignees_employees_EmployeeId\" TO \"FK_warehouse_order_assignees_employees_EmployeeId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_order_assignees_organizations_OrganizationId\" TO \"FK_warehouse_order_assignees_organizations_OrganizationId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_order_assignees_orders_OrderId\" TO \"FK_warehouse_order_assignees_warehouse_orders_WarehouseOrderId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"FK_order_picking_events_order_items_OrderItemId\" TO \"FK_warehouse_order_picking_events_warehouse_order_items_WarehouseOrderItemId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"FK_order_picking_events_orders_OrderId\" TO \"FK_warehouse_order_picking_events_warehouse_orders_WarehouseOrderId\";");

        migrationBuilder.AddForeignKey(
            name: "FK_pallet_items_warehouse_order_items_WarehouseOrderItemId",
            schema: "app",
            table: "pallet_items",
            column: "WarehouseOrderItemId",
            principalSchema: "app",
            principalTable: "warehouse_order_items",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pallet_items_warehouse_orders_WarehouseOrderId",
            schema: "app",
            table: "pallet_items",
            column: "WarehouseOrderId",
            principalSchema: "app",
            principalTable: "warehouse_orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pallets_warehouse_orders_WarehouseOrderId",
            schema: "app",
            table: "pallets",
            column: "WarehouseOrderId",
            principalSchema: "app",
            principalTable: "warehouse_orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_pallet_items_warehouse_order_items_WarehouseOrderItemId",
            schema: "app",
            table: "pallet_items");

        migrationBuilder.DropForeignKey(
            name: "FK_pallet_items_warehouse_orders_WarehouseOrderId",
            schema: "app",
            table: "pallet_items");

        migrationBuilder.DropForeignKey(
            name: "FK_pallets_warehouse_orders_WarehouseOrderId",
            schema: "app",
            table: "pallets");

        migrationBuilder.RenameIndex(
            name: "IX_warehouse_orders_Number", schema: "app", table: "warehouse_orders", newName: "IX_orders_Number");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_orders_Status_UpdatedAtUtc", schema: "app", table: "warehouse_orders", newName: "IX_orders_Status_UpdatedAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_items_WarehouseOrderId_ProductId", schema: "app", table: "warehouse_order_items", newName: "IX_order_items_OrderId_ProductId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_items_WarehouseOrderId_Status", schema: "app", table: "warehouse_order_items", newName: "IX_order_items_OrderId_Status");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_assignees_EmployeeId", schema: "app", table: "warehouse_order_assignees", newName: "IX_order_assignees_EmployeeId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_assignees_OrganizationId", schema: "app", table: "warehouse_order_assignees", newName: "IX_order_assignees_OrganizationId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_assignees_WarehouseOrderId_EmployeeId", schema: "app", table: "warehouse_order_assignees", newName: "IX_order_assignees_OrderId_EmployeeId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_picking_events_OperationId", schema: "app", table: "warehouse_order_picking_events", newName: "IX_order_picking_events_OperationId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_picking_events_WarehouseOrderItemId", schema: "app", table: "warehouse_order_picking_events", newName: "IX_order_picking_events_OrderItemId");
        migrationBuilder.RenameIndex(
            name: "IX_warehouse_order_picking_events_WarehouseOrderId_OccurredAtUtc", schema: "app", table: "warehouse_order_picking_events", newName: "IX_order_picking_events_OrderId_OccurredAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_pallets_WarehouseOrderId_ClosedAtUtc", schema: "app", table: "pallets", newName: "IX_pallets_OrderId_ClosedAtUtc");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_WarehouseOrderId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_OrderId");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_WarehouseOrderItemId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_OrderItemId");
        migrationBuilder.RenameIndex(
            name: "IX_pallet_items_PalletId_WarehouseOrderItemId", schema: "app", table: "pallet_items", newName: "IX_pallet_items_PalletId_OrderItemId");

        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"FK_warehouse_order_picking_events_warehouse_orders_WarehouseOrderId\" TO \"FK_order_picking_events_orders_OrderId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"FK_warehouse_order_picking_events_warehouse_order_items_WarehouseOrderItemId\" TO \"FK_order_picking_events_order_items_OrderItemId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_warehouse_order_assignees_warehouse_orders_WarehouseOrderId\" TO \"FK_order_assignees_orders_OrderId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_warehouse_order_assignees_organizations_OrganizationId\" TO \"FK_order_assignees_organizations_OrganizationId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"FK_warehouse_order_assignees_employees_EmployeeId\" TO \"FK_order_assignees_employees_EmployeeId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_items RENAME CONSTRAINT \"FK_warehouse_order_items_warehouse_orders_WarehouseOrderId\" TO \"FK_order_items_orders_OrderId\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_picking_events RENAME CONSTRAINT \"PK_warehouse_order_picking_events\" TO \"PK_order_picking_events\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_assignees RENAME CONSTRAINT \"PK_warehouse_order_assignees\" TO \"PK_order_assignees\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_order_items RENAME CONSTRAINT \"PK_warehouse_order_items\" TO \"PK_order_items\";");
        migrationBuilder.Sql("ALTER TABLE app.warehouse_orders RENAME CONSTRAINT \"PK_warehouse_orders\" TO \"PK_orders\";");

        migrationBuilder.RenameColumn(
            name: "WarehouseOrderId", schema: "app", table: "warehouse_order_items", newName: "OrderId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderId", schema: "app", table: "warehouse_order_assignees", newName: "OrderId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderId", schema: "app", table: "warehouse_order_picking_events", newName: "OrderId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderItemId", schema: "app", table: "warehouse_order_picking_events", newName: "OrderItemId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderId", schema: "app", table: "pallets", newName: "OrderId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderId", schema: "app", table: "pallet_items", newName: "OrderId");
        migrationBuilder.RenameColumn(
            name: "WarehouseOrderItemId", schema: "app", table: "pallet_items", newName: "OrderItemId");

        migrationBuilder.RenameTable(name: "warehouse_order_picking_events", schema: "app", newName: "order_picking_events");
        migrationBuilder.RenameTable(name: "warehouse_order_assignees", schema: "app", newName: "order_assignees");
        migrationBuilder.RenameTable(name: "warehouse_order_items", schema: "app", newName: "order_items");
        migrationBuilder.RenameTable(name: "warehouse_orders", schema: "app", newName: "orders");

        migrationBuilder.AddForeignKey(
            name: "FK_pallet_items_order_items_OrderItemId",
            schema: "app",
            table: "pallet_items",
            column: "OrderItemId",
            principalSchema: "app",
            principalTable: "order_items",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pallet_items_orders_OrderId",
            schema: "app",
            table: "pallet_items",
            column: "OrderId",
            principalSchema: "app",
            principalTable: "orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_pallets_orders_OrderId",
            schema: "app",
            table: "pallets",
            column: "OrderId",
            principalSchema: "app",
            principalTable: "orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
