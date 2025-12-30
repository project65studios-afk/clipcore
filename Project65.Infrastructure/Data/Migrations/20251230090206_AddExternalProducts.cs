using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project65.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalProducts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", nullable: true),
                    PriceDisplay = table.Column<string>(type: "TEXT", nullable: false),
                    CompareAtPriceDisplay = table.Column<string>(type: "TEXT", nullable: true),
                    ProductUrl = table.Column<string>(type: "TEXT", nullable: false),
                    BadgeText = table.Column<string>(type: "TEXT", nullable: true),
                    IsSoldOut = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventProducts",
                columns: table => new
                {
                    EventsId = table.Column<string>(type: "TEXT", nullable: false),
                    FeaturedProductsId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProducts", x => new { x.EventsId, x.FeaturedProductsId });
                    table.ForeignKey(
                        name: "FK_EventProducts_Events_EventsId",
                        column: x => x.EventsId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventProducts_ExternalProducts_FeaturedProductsId",
                        column: x => x.FeaturedProductsId,
                        principalTable: "ExternalProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventProducts_FeaturedProductsId",
                table: "EventProducts",
                column: "FeaturedProductsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventProducts");

            migrationBuilder.DropTable(
                name: "ExternalProducts");
        }
    }
}
