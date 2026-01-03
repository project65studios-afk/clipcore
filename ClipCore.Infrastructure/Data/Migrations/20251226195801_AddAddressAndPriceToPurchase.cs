using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndPriceToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricePaidCents",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PricePaidCents",
                table: "Purchases");
        }
    }
}
