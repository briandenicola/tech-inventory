using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceWarrantyExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "WarrantyExpiry",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarrantyExpiry",
                table: "Devices");
        }
    }
}
