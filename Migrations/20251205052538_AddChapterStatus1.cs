using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterStatus1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 5, 5, 25, 32, 848, DateTimeKind.Utc).AddTicks(4594), "$2a$11$xa5l9SjLjN8BUzYIAvoBiuLsvaPxeKfsz0n0vDF3lpYRFCX0kg/Oa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 12, 4, 6, 31, 40, 532, DateTimeKind.Utc).AddTicks(8908), "$2a$11$AMSDcBwtHu7m3NHO2Hd2x.toiEiIHUwuOEZ8e.ovQKR1y4NIjVnuW" });
        }
    }
}
