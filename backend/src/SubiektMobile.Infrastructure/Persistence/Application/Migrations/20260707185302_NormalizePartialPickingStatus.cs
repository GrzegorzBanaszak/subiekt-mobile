using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePartialPickingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE app.order_items
                SET "Status" = 'ToPick'
                WHERE "Status" = 'Packed'
                  AND "PackedQuantity" IS NOT NULL
                  AND "PackedQuantity" < "Quantity";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE app.order_items
                SET "Status" = 'Packed'
                WHERE "Status" = 'ToPick'
                  AND "PackedQuantity" IS NOT NULL
                  AND "PackedQuantity" < "Quantity";
                """);
        }
    }
}
