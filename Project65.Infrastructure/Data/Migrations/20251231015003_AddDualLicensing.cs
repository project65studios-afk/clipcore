using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDualLicensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_UserId_ClipId",
                table: "Purchases");

            migrationBuilder.AddColumn<int>(
                name: "LicenseType",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceCommercialCents",
                table: "Clips",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId_ClipId_LicenseType",
                table: "Purchases",
                columns: new[] { "UserId", "ClipId", "LicenseType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_UserId_ClipId_LicenseType",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PriceCommercialCents",
                table: "Clips");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId_ClipId",
                table: "Purchases",
                columns: new[] { "UserId", "ClipId" },
                unique: true);
        }
    }
}
