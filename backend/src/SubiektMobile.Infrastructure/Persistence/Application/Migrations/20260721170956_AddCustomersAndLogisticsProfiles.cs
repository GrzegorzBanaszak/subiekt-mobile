using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndLogisticsProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SubiektContractorId = table.Column<int>(type: "integer", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_sites",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_sites_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "app",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_logistics_profiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerSiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Street = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DefaultDock = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReceivingHours = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SupplierNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DefaultPalletType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MaximumPalletHeightCm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RequiresStretchFilm = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresStraps = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresCornerProtectors = table.Column<bool>(type: "boolean", nullable: false),
                    LoadSecuringNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LabelProfile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_logistics_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_logistics_profiles_customer_sites_CustomerSiteId",
                        column: x => x.CustomerSiteId,
                        principalSchema: "app",
                        principalTable: "customer_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_logistics_profiles_CustomerSiteId",
                schema: "app",
                table: "customer_logistics_profiles",
                column: "CustomerSiteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_sites_CustomerId_IsActive",
                schema: "app",
                table: "customer_sites",
                columns: new[] { "CustomerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_sites_CustomerId_NormalizedCode",
                schema: "app",
                table: "customer_sites",
                columns: new[] { "CustomerId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsActive_UpdatedAtUtc",
                schema: "app",
                table: "customers",
                columns: new[] { "IsActive", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_NormalizedCode",
                schema: "app",
                table: "customers",
                column: "NormalizedCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_logistics_profiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "customer_sites",
                schema: "app");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "app");
        }
    }
}
