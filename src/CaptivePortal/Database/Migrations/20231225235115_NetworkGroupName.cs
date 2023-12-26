using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class NetworkGroupName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomName",
                table: "NetworkGroups");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "NetworkGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "NetworkGroups");

            migrationBuilder.AddColumn<string>(
                name: "CustomName",
                table: "NetworkGroups",
                type: "TEXT",
                nullable: true);
        }
    }
}
