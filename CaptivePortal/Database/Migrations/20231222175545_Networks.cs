using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class Networks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceNetworkId",
                table: "Devices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Network",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NetworkAddress = table.Column<string>(type: "TEXT", nullable: false),
                    GatewayAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Cidr = table.Column<int>(type: "INTEGER", nullable: false),
                    Vlan = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Network", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceNetwork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    NetworkId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ManuallyAssignedAddress = table.Column<bool>(type: "INTEGER", nullable: false),
                    LeaseIssuedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNetwork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceNetwork_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceNetwork_Network_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Network",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNetwork_DeviceId",
                table: "DeviceNetwork",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNetwork_NetworkId",
                table: "DeviceNetwork",
                column: "NetworkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceNetwork");

            migrationBuilder.DropTable(
                name: "Network");

            migrationBuilder.DropColumn(
                name: "DeviceNetworkId",
                table: "Devices");
        }
    }
}
