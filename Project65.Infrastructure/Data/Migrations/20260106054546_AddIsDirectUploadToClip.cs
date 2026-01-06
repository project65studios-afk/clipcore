using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDirectUploadToClip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDirectUpload",
                table: "Clips",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDirectUpload",
                table: "Clips");
        }
    }
}
