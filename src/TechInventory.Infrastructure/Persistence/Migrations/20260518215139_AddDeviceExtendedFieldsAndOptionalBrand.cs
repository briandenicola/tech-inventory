using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceExtendedFieldsAndOptionalBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BrandId",
                table: "Devices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Devices",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Devices",
                type: "TEXT",
                maxLength: 17,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "Devices",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductUrl",
                table: "Devices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Devices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Devices",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ProductUrl",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Devices");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrandId",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
