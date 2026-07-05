using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPickingAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickingMode",
                schema: "app",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SingleAssignee");

            migrationBuilder.CreateTable(
                name: "order_assignees",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeDisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_assignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_assignees_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_assignees_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "app",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_assignees_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "app",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_assignees_EmployeeId",
                schema: "app",
                table: "order_assignees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_order_assignees_OrderId_EmployeeId",
                schema: "app",
                table: "order_assignees",
                columns: new[] { "OrderId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_assignees_OrganizationId",
                schema: "app",
                table: "order_assignees",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_assignees",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "PickingMode",
                schema: "app",
                table: "orders");
        }
    }
}
