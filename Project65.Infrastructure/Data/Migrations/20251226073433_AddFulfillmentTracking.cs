using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FulfilledAt",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentStatus",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HighResDownloadUrl",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterFileName",
                table: "Clips",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfilledAt",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "FulfillmentStatus",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "HighResDownloadUrl",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "MasterFileName",
                table: "Clips");
        }
    }
}
