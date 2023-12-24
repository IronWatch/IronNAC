using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class DeviceNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NickName",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NickName",
                table: "Devices");
        }
    }
}
