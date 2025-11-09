using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class updateRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinUnits = table.Column<int>(type: "int", nullable: false),
                    Deposit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LateFeePerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentalPricingTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ThresholdDays = table.Column<int>(type: "int", nullable: false),
                    PricePerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalPricingTiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalPlans_ProductId_Unit",
                table: "RentalPlans",
                columns: new[] { "ProductId", "Unit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalPricingTiers_ProductId_ThresholdDays",
                table: "RentalPricingTiers",
                columns: new[] { "ProductId", "ThresholdDays" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalPlans");

            migrationBuilder.DropTable(
                name: "RentalPricingTiers");
        }
    }
}
