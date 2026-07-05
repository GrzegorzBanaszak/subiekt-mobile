using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class RequireAdministratorPasswordChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordChange",
                schema: "app",
                table: "administrators",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                schema: "app",
                table: "administrators");
        }
    }
}
