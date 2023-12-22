using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class Devices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthorizedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeviceMac = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceIpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    NasIpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    NasIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    CallingStationId = table.Column<string>(type: "TEXT", nullable: true),
                    AccountingSessionId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PersonId",
                table: "Devices",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
