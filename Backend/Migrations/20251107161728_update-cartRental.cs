using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class updatecartRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalDeposit",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalEndDate",
                table: "CartItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalLateFeePerDay",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalPricePerDay",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalStartDate",
                table: "CartItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RentalUnits",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalDeposit",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalEndDate",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalLateFeePerDay",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalPricePerDay",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalStartDate",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RentalUnits",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CartItems");
        }
    }
}
