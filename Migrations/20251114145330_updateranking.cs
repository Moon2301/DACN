using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN.Migrations
{
    /// <inheritdoc />
    public partial class updateranking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GenreName",
                table: "StoryRankings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "AvatarUrl", "CreatedAt", "PasswordHash", "Ticket" },
                values: new object[] { "/images/defaul/defaulAvatar.png", new DateTime(2025, 11, 14, 14, 53, 28, 198, DateTimeKind.Utc).AddTicks(1909), "$2a$11$0o6xws6xNKJZ2BX9oA0z0urUXHZ/Mv6QJM6NgbkUzOWVpnVl/Zjfa", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenreName",
                table: "StoryRankings");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "AvatarUrl", "CreatedAt", "PasswordHash", "Ticket" },
                values: new object[] { "/images/defaulAvatar.png", new DateTime(2025, 11, 13, 7, 20, 48, 947, DateTimeKind.Utc).AddTicks(1710), "$2a$11$FOf9K14NDrvEPRaywww6EuChaYDmzuHTtzgIKzILo0bdKzY7KHoMq", 999999 });
        }
    }
}
