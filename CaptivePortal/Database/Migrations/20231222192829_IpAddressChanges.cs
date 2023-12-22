using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class IpAddressChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeviceIpAddress",
                table: "Devices",
                newName: "DetectedDeviceIpAddress");

            migrationBuilder.RenameColumn(
                name: "DeviceAddress",
                table: "DeviceNetwork",
                newName: "AssignedDeviceAddress");

            migrationBuilder.AddColumn<bool>(
                name: "Authorized",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authorized",
                table: "Devices");

            migrationBuilder.RenameColumn(
                name: "DetectedDeviceIpAddress",
                table: "Devices",
                newName: "DeviceIpAddress");

            migrationBuilder.RenameColumn(
                name: "AssignedDeviceAddress",
                table: "DeviceNetwork",
                newName: "DeviceAddress");
        }
    }
}
