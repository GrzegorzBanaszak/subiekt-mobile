using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubiektMobile.Infrastructure.Persistence.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packaging_types",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TareWeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DefaultCapacity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packaging_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_packaging_codes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackagingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_packaging_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_packaging_codes_customer_sites_CustomerSiteId",
                        column: x => x.CustomerSiteId,
                        principalSchema: "app",
                        principalTable: "customer_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_packaging_codes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "app",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_packaging_codes_packaging_types_PackagingTypeId",
                        column: x => x.PackagingTypeId,
                        principalSchema: "app",
                        principalTable: "packaging_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_part_mappings",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerPartNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedCustomerPartNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DefaultPackagingTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EngineeringChange = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_part_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_part_mappings_customer_sites_CustomerSiteId",
                        column: x => x.CustomerSiteId,
                        principalSchema: "app",
                        principalTable: "customer_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_part_mappings_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "app",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_part_mappings_packaging_types_DefaultPackagingType~",
                        column: x => x.DefaultPackagingTypeId,
                        principalSchema: "app",
                        principalTable: "packaging_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_packaging_codes_CustomerId_NormalizedCode",
                schema: "app",
                table: "customer_packaging_codes",
                columns: new[] { "CustomerId", "NormalizedCode" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packaging_codes_CustomerId_PackagingTypeId",
                schema: "app",
                table: "customer_packaging_codes",
                columns: new[] { "CustomerId", "PackagingTypeId" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packaging_codes_CustomerSiteId_NormalizedCode",
                schema: "app",
                table: "customer_packaging_codes",
                columns: new[] { "CustomerSiteId", "NormalizedCode" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packaging_codes_CustomerSiteId_PackagingTypeId",
                schema: "app",
                table: "customer_packaging_codes",
                columns: new[] { "CustomerSiteId", "PackagingTypeId" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_packaging_codes_PackagingTypeId",
                schema: "app",
                table: "customer_packaging_codes",
                column: "PackagingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_part_mappings_CustomerId_CustomerSiteId_IsActive",
                schema: "app",
                table: "customer_part_mappings",
                columns: new[] { "CustomerId", "CustomerSiteId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_part_mappings_CustomerId_NormalizedCustomerPartNum~",
                schema: "app",
                table: "customer_part_mappings",
                columns: new[] { "CustomerId", "NormalizedCustomerPartNumber" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_part_mappings_CustomerSiteId_NormalizedCustomerPar~",
                schema: "app",
                table: "customer_part_mappings",
                columns: new[] { "CustomerSiteId", "NormalizedCustomerPartNumber" },
                unique: true,
                filter: "\"CustomerSiteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_part_mappings_DefaultPackagingTypeId",
                schema: "app",
                table: "customer_part_mappings",
                column: "DefaultPackagingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_packaging_types_IsActive_UpdatedAtUtc",
                schema: "app",
                table: "packaging_types",
                columns: new[] { "IsActive", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_packaging_types_NormalizedCode",
                schema: "app",
                table: "packaging_types",
                column: "NormalizedCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_packaging_codes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "customer_part_mappings",
                schema: "app");

            migrationBuilder.DropTable(
                name: "packaging_types",
                schema: "app");
        }
    }
}
