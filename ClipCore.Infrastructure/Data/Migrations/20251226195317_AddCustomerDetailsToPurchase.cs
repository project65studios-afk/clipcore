using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDetailsToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Purchases",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Purchases");
        }
    }
}
