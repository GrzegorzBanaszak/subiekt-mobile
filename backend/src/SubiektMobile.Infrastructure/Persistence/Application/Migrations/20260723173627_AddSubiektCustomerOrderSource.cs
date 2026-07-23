using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddSubiektCustomerOrderSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubiektSourceDocumentId",
                schema: "app",
                table: "warehouse_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubiektSourceDocumentNumber",
                schema: "app",
                table: "warehouse_orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubiektSourceItemId",
                schema: "app",
                table: "warehouse_order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_orders_SubiektSourceDocumentId",
                schema: "app",
                table: "warehouse_orders",
                column: "SubiektSourceDocumentId",
                unique: true,
                filter: "\"SubiektSourceDocumentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_order_items_SubiektSourceItemId",
                schema: "app",
                table: "warehouse_order_items",
                column: "SubiektSourceItemId",
                unique: true,
                filter: "\"SubiektSourceItemId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_warehouse_orders_SubiektSourceDocumentId",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropIndex(
                name: "IX_warehouse_order_items_SubiektSourceItemId",
                schema: "app",
                table: "warehouse_order_items");

            migrationBuilder.DropColumn(
                name: "SubiektSourceDocumentId",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropColumn(
                name: "SubiektSourceDocumentNumber",
                schema: "app",
                table: "warehouse_orders");

            migrationBuilder.DropColumn(
                name: "SubiektSourceItemId",
                schema: "app",
                table: "warehouse_order_items");
        }
    }
}
