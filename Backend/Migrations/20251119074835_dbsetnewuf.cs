using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class dbsetnewuf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Vounchers",
                keyColumn: "Id",
                keyValue: 10,
                column: "MinimumOrderValue",
                value: 200000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Vounchers",
                keyColumn: "Id",
                keyValue: 10,
                column: "MinimumOrderValue",
                value: 2000000m);
        }
    }
}
