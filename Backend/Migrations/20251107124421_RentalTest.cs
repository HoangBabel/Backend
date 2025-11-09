using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RentalTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RentalDays",
                table: "RentalItems",
                newName: "Units");

            migrationBuilder.RenameColumn(
                name: "PricePerDay",
                table: "RentalItems",
                newName: "PricePerUnitAtBooking");

            migrationBuilder.AddColumn<decimal>(
                name: "CleaningFee",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DamageFee",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPaid",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositRefund",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFee",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "Rentals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Rentals",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAtBooking",
                table: "RentalItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeePerUnitAtBooking",
                table: "RentalItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRental",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 1,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 2,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 3,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 4,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 5,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 6,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 7,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 8,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 9,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "IdProduct",
                keyValue: 10,
                columns: new[] { "Condition", "IsRental" },
                values: new object[] { 0, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleaningFee",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DamageFee",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DepositPaid",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DepositRefund",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "LateFee",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DepositAtBooking",
                table: "RentalItems");

            migrationBuilder.DropColumn(
                name: "LateFeePerUnitAtBooking",
                table: "RentalItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsRental",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Units",
                table: "RentalItems",
                newName: "RentalDays");

            migrationBuilder.RenameColumn(
                name: "PricePerUnitAtBooking",
                table: "RentalItems",
                newName: "PricePerDay");
        }
    }
}
