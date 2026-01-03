using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DecouplePurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Clips_ClipId",
                table: "Purchases");

            migrationBuilder.AlterColumn<string>(
                name: "ClipId",
                table: "Purchases",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ClipTitle",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Clips_ClipId",
                table: "Purchases",
                column: "ClipId",
                principalTable: "Clips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Clips_ClipId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ClipTitle",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EventName",
                table: "Purchases");

            migrationBuilder.AlterColumn<string>(
                name: "ClipId",
                table: "Purchases",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Clips_ClipId",
                table: "Purchases",
                column: "ClipId",
                principalTable: "Clips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
