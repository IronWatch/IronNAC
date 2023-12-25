using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptivePortal.Database.Migrations
{
    /// <inheritdoc />
    public partial class NetworkGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Networks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NetworkGroupId",
                table: "Networks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NetworkGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Registration = table.Column<bool>(type: "INTEGER", nullable: false),
                    Guest = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNetworkGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    NetworkGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNetworkGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNetworkGroups_NetworkGroups_NetworkGroupId",
                        column: x => x.NetworkGroupId,
                        principalTable: "NetworkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNetworkGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Networks_NetworkGroupId",
                table: "Networks",
                column: "NetworkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkGroups_NetworkGroupId",
                table: "UserNetworkGroups",
                column: "NetworkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkGroups_UserId",
                table: "UserNetworkGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_NetworkGroups_NetworkGroupId",
                table: "Networks",
                column: "NetworkGroupId",
                principalTable: "NetworkGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Networks_NetworkGroups_NetworkGroupId",
                table: "Networks");

            migrationBuilder.DropTable(
                name: "UserNetworkGroups");

            migrationBuilder.DropTable(
                name: "NetworkGroups");

            migrationBuilder.DropIndex(
                name: "IX_Networks_NetworkGroupId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "NetworkGroupId",
                table: "Networks");
        }
    }
}
