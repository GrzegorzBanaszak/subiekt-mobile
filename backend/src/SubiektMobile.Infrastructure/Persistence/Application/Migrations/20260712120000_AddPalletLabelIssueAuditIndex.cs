using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPalletLabelIssueAuditIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_TargetType_TargetId_Action_OccurredAtUtc",
                schema: "app",
                table: "audit_entries",
                columns: new[] { "TargetType", "TargetId", "Action", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_entries_TargetType_TargetId_Action_OccurredAtUtc",
                schema: "app",
                table: "audit_entries");
        }
    }
}
