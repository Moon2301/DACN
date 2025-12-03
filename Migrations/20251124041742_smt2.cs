using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN.Migrations
{
    /// <inheritdoc />
    public partial class smt2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChapterReadedByUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 24, 4, 17, 39, 575, DateTimeKind.Utc).AddTicks(1406), "$2a$11$4fq1EhyHXg2MGVkvnmdG4eyi/tuh4D1was8mKxXWliF3yeaqJNLfq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChapterReadedByUsers");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 14, 14, 53, 28, 198, DateTimeKind.Utc).AddTicks(1909), "$2a$11$0o6xws6xNKJZ2BX9oA0z0urUXHZ/Mv6QJM6NgbkUzOWVpnVl/Zjfa" });
        }
    }
}
