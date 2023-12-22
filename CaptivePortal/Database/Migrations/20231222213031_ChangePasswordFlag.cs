using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangePasswordFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChangePasswordNextLogin",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangePasswordNextLogin",
                table: "Users");
        }
    }
}
