using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 4, 6, 31, 40, 532, DateTimeKind.Utc).AddTicks(8908), "$2a$11$AMSDcBwtHu7m3NHO2Hd2x.toiEiIHUwuOEZ8e.ovQKR1y4NIjVnuW" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Chapters");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 24, 4, 17, 39, 575, DateTimeKind.Utc).AddTicks(1406), "$2a$11$4fq1EhyHXg2MGVkvnmdG4eyi/tuh4D1was8mKxXWliF3yeaqJNLfq" });
        }
    }
}
