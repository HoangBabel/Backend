using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class updateUser1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "PhoneNumber", "Role", "Username" },
                values: new object[] { 10, new DateTime(2025, 10, 16, 10, 32, 39, 924, DateTimeKind.Local).AddTicks(5745), "giang@example.com", "le giang", true, "$2a$11$zB0cPctNLMkRJqNbC7qc7eF.VvtXVr1KmCuGUEoXC331zdp4Q9J.a", "0773678161", "Admin", "giang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
