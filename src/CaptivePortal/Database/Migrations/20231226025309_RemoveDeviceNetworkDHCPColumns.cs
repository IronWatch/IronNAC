using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceNetworkDHCPColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDeviceAddress",
                table: "DeviceNetworks");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "DeviceNetworks");

            migrationBuilder.DropColumn(
                name: "LeaseIssuedAt",
                table: "DeviceNetworks");

            migrationBuilder.DropColumn(
                name: "ManuallyAssignedAddress",
                table: "DeviceNetworks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDeviceAddress",
                table: "DeviceNetworks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAt",
                table: "DeviceNetworks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseIssuedAt",
                table: "DeviceNetworks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ManuallyAssignedAddress",
                table: "DeviceNetworks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
