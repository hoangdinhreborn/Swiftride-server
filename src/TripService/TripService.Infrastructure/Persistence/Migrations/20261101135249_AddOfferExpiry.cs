using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OfferedAt",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OfferedAt",
                table: "Trips",
                column: "OfferedAt",
                filter: "\"Status\" = 'DriverOffered'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_OfferedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OfferedAt",
                table: "Trips");
        }
    }
}
