using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class AddGIFSalesFeaturesPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Events table changes
            migrationBuilder.AddColumn<bool>(
                name: "DefaultAllowGifSale",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultGifPriceCents",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 199);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPriceCents",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPriceCommercialCents",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 4900);

            // Clips table changes
            migrationBuilder.AddColumn<bool>(
                name: "AllowGifSale",
                table: "Clips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GifPriceCents",
                table: "Clips",
                type: "integer",
                nullable: false,
                defaultValue: 199);

            // Purchases table changes
            migrationBuilder.AddColumn<bool>(
                name: "IsGif",
                table: "Purchases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "GifStartTime",
                table: "Purchases",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GifEndTime",
                table: "Purchases",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandedPlaybackId",
                table: "Purchases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAllowGifSale",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DefaultGifPriceCents",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DefaultPriceCents",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DefaultPriceCommercialCents",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AllowGifSale",
                table: "Clips");

            migrationBuilder.DropColumn(
                name: "GifPriceCents",
                table: "Clips");

            migrationBuilder.DropColumn(
                name: "IsGif",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "GifStartTime",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "GifEndTime",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "BrandedPlaybackId",
                table: "Purchases");
        }
    }
}
