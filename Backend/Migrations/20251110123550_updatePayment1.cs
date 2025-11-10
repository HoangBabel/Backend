using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class updatePayment1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentLinkId",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentLinkId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrderCode",
                table: "Payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentLinkId",
                table: "Payments",
                column: "PaymentLinkId",
                unique: true,
                filter: "[PaymentLinkId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentLinkId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentLinkId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentLinkId",
                table: "Payments",
                column: "PaymentLinkId",
                unique: true);
        }
    }
}
