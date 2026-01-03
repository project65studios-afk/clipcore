using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClipDurationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ClipDurationSec",
                table: "Purchases",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClipDurationSec",
                table: "Purchases");
        }
    }
}
