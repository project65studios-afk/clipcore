using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueSessionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_StripeSessionId",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_StripeSessionId",
                table: "Purchases",
                column: "StripeSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_StripeSessionId",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_StripeSessionId",
                table: "Purchases",
                column: "StripeSessionId",
                unique: true);
        }
    }
}
