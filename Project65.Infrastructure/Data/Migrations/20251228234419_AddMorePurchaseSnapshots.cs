using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMorePurchaseSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClipRecordingStartedAt",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EventDate",
                table: "Purchases",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClipRecordingStartedAt",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "Purchases");
        }
    }
}
