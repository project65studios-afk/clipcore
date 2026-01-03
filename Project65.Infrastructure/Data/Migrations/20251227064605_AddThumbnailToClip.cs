using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailToClip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailFileName",
                table: "Clips",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailFileName",
                table: "Clips");
        }
    }
}
