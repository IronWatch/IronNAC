using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoreNetworkChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cidr",
                table: "Networks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cidr",
                table: "Networks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
