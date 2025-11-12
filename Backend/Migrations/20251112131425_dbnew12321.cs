using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class dbnew12321 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentLinkId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderCode = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RefId = table.Column<int>(type: "int", nullable: false),
                    ExpectedAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QrCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEventAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    TwoFactorCodeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vounchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinimumOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyToShipping = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShippingDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: false),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vounchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    IdProduct = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    IsRental = table.Column<bool>(type: "bit", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.IdProduct);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCheckedOut = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ToProvinceId = table.Column<int>(type: "int", nullable: true),
                    ToProvinceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToDistrictId = table.Column<int>(type: "int", nullable: true),
                    ToDistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToWardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToWardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: true),
                    Length = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    VoucherCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Vounchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vounchers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rentals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ToProvinceId = table.Column<int>(type: "int", nullable: true),
                    ToProvinceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToDistrictId = table.Column<int>(type: "int", nullable: true),
                    ToDistrictName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToWardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToWardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: true),
                    Length = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    VoucherCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CleaningFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DamageFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositRefund = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rentals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rentals_Vounchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vounchers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "IdProduct",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "IdProduct",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentalId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PricePerUnitAtBooking = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAtBooking = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LateFeePerUnitAtBooking = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "IdProduct",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalItems_Rentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Name" },
                values: new object[,]
                {
                    { 1, "Tivi" },
                    { 2, "Tủ lạnh" },
                    { 3, "Máy giặt" },
                    { 4, "Máy lạnh" },
                    { 5, "Gia dụng nhà bếp" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "IsTwoFactorEnabled", "PasswordHash", "PhoneNumber", "Role", "TwoFactorCode", "TwoFactorCodeExpiry", "Username" },
                values: new object[] { 10, new DateTime(2025, 10, 16, 10, 32, 39, 924, DateTimeKind.Local).AddTicks(5745), "giang@example.com", "le giang", true, false, "$2a$11$zB0cPctNLMkRJqNbC7qc7eF.VvtXVr1KmCuGUEoXC331zdp4Q9J.a", "0773678161", "Admin", null, null, "giang" });

            migrationBuilder.InsertData(
                table: "Vounchers",
                columns: new[] { "Id", "ApplyToShipping", "Code", "CurrentUsageCount", "DiscountPercent", "DiscountValue", "ExpirationDate", "IsValid", "MaxUsageCount", "MaximumDiscount", "MinimumOrderValue", "ShippingDiscountPercent", "Type", "UsedAt" },
                values: new object[,]
                {
                    { 1, false, "GIAM500K", 0, null, 500000m, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100, null, 5000000m, null, "fixed", null },
                    { 2, false, "GIAM1M", 0, null, 1000000m, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 50, null, 10000000m, null, "fixed", null },
                    { 3, false, "GIAM10", 0, 10m, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 200, 500000m, 2000000m, null, "percent", null },
                    { 4, false, "GIAM20", 0, 20m, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100, 1000000m, 5000000m, null, "percent", null },
                    { 5, false, "GIAM30", 0, 30m, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 30, 2000000m, 10000000m, null, "percent", null },
                    { 6, true, "FREESHIP", 0, null, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 500, null, 1000000m, null, "shipping", null },
                    { 7, true, "SHIP50", 0, null, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 300, null, 500000m, 50m, "shipping", null },
                    { 8, false, "COMBO200K", 0, null, 200000m, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 150, null, 3000000m, null, "fixed", null },
                    { 9, false, "THUE100K", 0, null, 100000m, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 200, null, 1000000m, null, "fixed", null },
                    { 10, false, "THUE15", 0, 15m, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100, 300000m, 2000000m, null, "percent", null },
                    { 11, false, "EXPIRED", 0, null, 500000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 10, null, 1000000m, null, "fixed", null },
                    { 12, false, "SOLDOUT", 5, 25m, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 5, 1000000m, 3000000m, null, "percent", null },
                    { 13, true, "COMBOSHIP", 0, null, null, new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 150, null, 3000000m, null, "shipping", null }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "IdProduct", "CategoryId", "Condition", "CreatedBy", "CreatedDate", "Description", "Image", "IsDeleted", "IsRental", "Name", "Price", "Quantity", "Status", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, 0, null, null, "Smart Tivi 4K hiển thị sắc nét, hỗ trợ điều khiển bằng giọng nói.", null, false, false, "Tivi Samsung Crystal UHD 55 inch BU8000", 12990000m, 12, "ConHang", null, null },
                    { 2, 1, 0, null, null, "Màn hình OLED siêu mỏng, hỗ trợ Dolby Vision và Dolby Atmos.", null, false, false, "Tivi LG OLED evo 48 inch C3", 27990000m, 5, "ConHang", null, null },
                    { 3, 2, 0, null, null, "Công nghệ tiết kiệm điện Inverter, ngăn đông mềm Prime Fresh+.", null, false, false, "Tủ lạnh Panasonic Inverter 322 lít NR-BV361BPKV", 12490000m, 8, "ConHang", null, null },
                    { 4, 2, 0, null, null, "Làm lạnh nhanh, khử mùi bằng than hoạt tính, thiết kế sang trọng.", null, false, false, "Tủ lạnh Samsung Inverter 424 lít RT42CG6324B1SV", 14990000m, 7, "ConHang", null, null },
                    { 5, 3, 0, null, null, "Công nghệ UltraMix hòa tan bột giặt, giảm phai màu quần áo.", null, false, false, "Máy giặt Electrolux Inverter 10kg EWF1024BDWA", 8990000m, 10, "ConHang", null, null },
                    { 6, 3, 0, null, null, "AI Control tự động tối ưu chương trình giặt, tiết kiệm năng lượng.", null, false, false, "Máy giặt Samsung Inverter 9.5kg WW95T504DAW/SV", 8790000m, 6, "ConHang", null, null },
                    { 7, 4, 0, null, null, "Công nghệ Streamer khử khuẩn, tiết kiệm điện vượt trội.", null, false, false, "Máy lạnh Daikin Inverter 1.5 HP FTKY35WMVMV", 11490000m, 9, "ConHang", null, null },
                    { 8, 4, 0, null, null, "Làm lạnh nhanh, kháng khuẩn bằng ion bạc, vận hành êm ái.", null, false, false, "Máy lạnh LG Inverter 1 HP V10WIN", 8390000m, 11, "ConHang", null, null },
                    { 9, 5, 0, null, null, "Công nghệ Rapid Air giảm 90% dầu mỡ, vỏ thép sơn tĩnh điện.", null, false, false, "Nồi chiên không dầu Philips HD9200/90 4.1L", 2290000m, 15, "ConHang", null, null },
                    { 10, 5, 0, null, null, "Chức năng hâm, nấu, rã đông nhanh, điều khiển núm xoay cơ học.", null, false, false, "Lò vi sóng Sharp R-G226VN-BK 20 lít", 1990000m, 14, "ConHang", null, null },
                    { 11, 1, 1, null, null, "Smart Tivi Full HD 43 inch cho thuê theo tháng, phù hợp sự kiện, văn phòng.", null, false, true, "Tivi Samsung 43 inch (Cho thuê)", 500000m, 20, "ConHang", null, null },
                    { 12, 1, 1, null, null, "Smart Tivi 4K 55 inch cho thuê, hỗ trợ lắp đặt tận nơi.", null, false, true, "Tivi LG 55 inch 4K (Cho thuê)", 800000m, 15, "ConHang", null, null },
                    { 13, 2, 1, null, null, "Tủ lạnh mini cho thuê theo tháng, phù hợp phòng trọ, sinh viên.", null, false, true, "Tủ lạnh 180 lít (Cho thuê)", 400000m, 25, "ConHang", null, null },
                    { 14, 2, 1, null, null, "Tủ lạnh Inverter tiết kiệm điện cho thuê, bảo hành trong thời gian thuê.", null, false, true, "Tủ lạnh Inverter 350 lít (Cho thuê)", 700000m, 12, "ConHang", null, null },
                    { 15, 3, 1, null, null, "Máy giặt cửa trên 8kg cho thuê, phù hợp gia đình nhỏ.", null, false, true, "Máy giặt 8kg (Cho thuê)", 450000m, 18, "ConHang", null, null },
                    { 16, 3, 1, null, null, "Máy giặt Inverter tiết kiệm điện, vận hành êm ái cho thuê theo tháng.", null, false, true, "Máy giặt Inverter 9kg (Cho thuê)", 600000m, 10, "ConHang", null, null },
                    { 17, 4, 1, null, null, "Máy lạnh 1 HP cho thuê theo tháng, bảo trì miễn phí.", null, false, true, "Máy lạnh 1 HP (Cho thuê)", 550000m, 30, "ConHang", null, null },
                    { 18, 4, 1, null, null, "Máy lạnh Inverter tiết kiệm điện, làm lạnh nhanh cho thuê.", null, false, true, "Máy lạnh Inverter 1.5 HP (Cho thuê)", 750000m, 22, "ConHang", null, null },
                    { 19, 5, 1, null, null, "Lò vi sóng cho thuê theo tháng, phù hợp văn phòng, phòng trọ.", null, false, true, "Lò vi sóng 20L (Cho thuê)", 200000m, 25, "ConHang", null, null },
                    { 20, 5, 1, null, null, "Nồi chiên không dầu dung tích lớn cho thuê, phù hợp gia đình.", null, false, true, "Nồi chiên không dầu 5L (Cho thuê)", 25000m, 20, "ConHang", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId_NotCheckedOut",
                table: "Carts",
                column: "UserId",
                unique: true,
                filter: "[IsCheckedOut] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VoucherId",
                table: "Orders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentLinkId",
                table: "Payments",
                column: "PaymentLinkId",
                unique: true,
                filter: "[PaymentLinkId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_ProductId",
                table: "RentalItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_RentalId",
                table: "RentalItems",
                column: "RentalId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_UserId",
                table: "Rentals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_VoucherId",
                table: "Rentals",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RentalItems");

            migrationBuilder.DropTable(
                name: "RentalPlans");

            migrationBuilder.DropTable(
                name: "RentalPricingTiers");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Rentals");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vounchers");
        }
    }
}
