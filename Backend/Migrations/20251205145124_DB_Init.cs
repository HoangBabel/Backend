using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class DB_Init : Migration
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
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    ResetPasswordCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPasswordCodeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    TwoFactorCodeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorAttemptCount = table.Column<int>(type: "int", nullable: false)
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
                    VoucherCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentLinkId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QrCodeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    PaymentLinkId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QrCodeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", nullable: false),
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
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ImageUrls = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LikeCount = table.Column<int>(type: "int", nullable: false),
                    ParentReviewId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "IdProduct",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Reviews_ParentReviewId",
                        column: x => x.ParentReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    { 1, "Chế biến thực phẩm" },
                    { 2, "Làm mát và giữ lạnh" },
                    { 3, "Giặt giũ và vệ sinh" },
                    { 4, "Giải trí" },
                    { 5, "Chăm sóc cá nhân" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "AvatarUrl", "CreatedAt", "Email", "FullName", "IsActive", "IsTwoFactorEnabled", "PasswordHash", "PhoneNumber", "ResetPasswordCode", "ResetPasswordCodeExpiry", "Role", "TwoFactorAttemptCount", "TwoFactorCode", "TwoFactorCodeExpiry", "Username" },
                values: new object[,]
                {
                    { 1, null, "https://localhost:44303/uploads/avatars/d580bd4e-3fce-4964-9e0c-53177f34082c.png", new DateTime(2025, 11, 11, 22, 27, 39, 707, DateTimeKind.Local).AddTicks(2879), "hoangphap1000@gmail.com", "Lê Hoàng Pháp", true, false, "$2a$11$5WvpePUu2EIg8jo7MBWjvee3/uwro4V6QUIRSAju3HSEJVmvwcXJe", "0564090866", null, null, "Admin", 0, null, null, "admin" },
                    { 2, null, null, new DateTime(2025, 10, 16, 10, 32, 39, 924, DateTimeKind.Local).AddTicks(5745), "giang@example.com", "Le Giang", true, false, "$2a$11$zB0cPctNLMkRJqNbC7qc7eF.VvtXVr1KmCuGUEoXC331zdp4Q9J.a", "0773678161", null, null, "Admin", 0, null, null, "giang" }
                });

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
                    { 1, 1, 0, null, null, "Nấu cơm nhanh, giữ ấm lâu, dễ vệ sinh.", "/images/giadung1.jpg", false, true, "Nồi cơm điện Cuckoo CR-0675F", 1590000m, 18, "ConHang", null, null },
                    { 2, 1, 0, null, null, "Công nghệ Rapid Air giảm 90% dầu mỡ.", "/images/giadung2.jpg", false, false, "Nồi chiên không dầu Philips HD9200/90", 2290000m, 15, "ConHang", null, null },
                    { 3, 1, 0, null, null, "Hâm, nấu, rã đông nhanh, núm xoay cơ học.", "/images/giadung3.jpg", false, true, "Lò vi sóng Sharp R-G226VN-BK", 1990000m, 14, "ConHang", null, null },
                    { 4, 1, 0, null, null, "Công suất 700W, lưỡi dao thép không gỉ.", "/images/giadung4.jpg", false, false, "Máy xay sinh tố Philips HR2221/00", 1250000m, 12, "ConHang", null, null },
                    { 5, 1, 0, null, null, "Pha espresso chất lượng, thiết kế nhỏ gọn.", "/images/giadung5.jpg", false, true, "Máy pha cà phê Delonghi EC685", 3990000m, 10, "ConHang", null, null },
                    { 6, 2, 0, null, null, "Làm lạnh nhanh, khử mùi than hoạt tính.", "/images/lammat1.jpg", false, false, "Tủ lạnh Samsung Inverter 424 lít RT42CG6324B1SV", 14990000m, 7, "ConHang", null, null },
                    { 7, 2, 0, null, null, "Bảo quản thực phẩm lâu dài, tiết kiệm điện.", "/images/lammat2.jpg", false, true, "Tủ đông Electrolux 200 lít EFZ2200H-H", 8990000m, 8, "ConHang", null, null },
                    { 8, 2, 0, null, null, "Làm mát nhanh, lọc bụi, 3 chế độ gió.", "/images/lammat3.jpg", false, false, "Quạt điều hòa Midea AC120-19AR", 3290000m, 12, "ConHang", null, null },
                    { 9, 2, 0, null, null, "Khử khuẩn, tiết kiệm điện vượt trội.", "/images/lammat4.jpg", false, true, "Máy điều hòa Daikin Inverter 1.5 HP FTKY35WMVMV", 11490000m, 9, "ConHang", null, null },
                    { 10, 2, 0, null, null, "Tiện dụng, vận hành êm, thiết kế nhỏ gọn.", "/images/lammat5.jpg", false, false, "Quạt điện Asia F16001", 550000m, 20, "ConHang", null, null },
                    { 11, 3, 0, null, null, "Công nghệ UltraMix hòa tan bột giặt.", "/images/giatgiu1.jpg", false, true, "Máy giặt Electrolux Inverter 10kg EWF1024BDWA", 8990000m, 10, "ConHang", null, null },
                    { 12, 3, 0, null, null, "Rửa sạch, tiết kiệm nước, vận hành êm ái.", "/images/giatgiu2.jpg", false, false, "Máy rửa chén Bosch SMS25CI00E", 12990000m, 6, "ConHang", null, null },
                    { 13, 3, 0, null, null, "Hút bụi tự động, lập bản đồ thông minh.", "/images/giatgiu3.jpg", false, true, "Robot hút bụi Xiaomi Mi Robot Vacuum", 4990000m, 15, "ConHang", null, null },
                    { 14, 3, 0, null, null, "Loại bỏ bụi mịn, vi khuẩn và mùi hôi.", "/images/giatgiu4.jpg", false, false, "Máy lọc không khí Sharp FP-J40E-B", 3990000m, 8, "ConHang", null, null },
                    { 15, 3, 0, null, null, "Sấy nhanh, tiết kiệm điện, bảo vệ vải.", "/images/giatgiu5.jpg", false, true, "Máy sấy quần áo Electrolux EDV705HQWA", 5990000m, 9, "ConHang", null, null },
                    { 16, 4, 0, null, null, "Smart Tivi 4K sắc nét, hỗ trợ giọng nói.", "/images/giai_tri1.jpg", false, false, "Tivi Samsung Crystal UHD 55 inch BU8000", 12990000m, 12, "ConHang", null, null },
                    { 17, 4, 0, null, null, "Âm thanh mạnh mẽ, pin 20 giờ.", "/images/giai_tri2.jpg", false, true, "Loa Bluetooth JBL Charge 5", 2590000m, 15, "ConHang", null, null },
                    { 18, 4, 0, null, null, "Màn hình 4K sắc nét, hệ điều hành webOS, hỗ trợ điều khiển giọng nói.", "/images/giai_tri3.jpg", false, false, "Tivi LG 4K Smart TV 55 inch UQ7550", 11990000m, 10, "ConHang", null, null },
                    { 19, 4, 0, null, null, "Chiếu phim Full HD, pin tích hợp.", "/images/giai_tri4.jpg", false, true, "Máy chiếu mini ViewSonic M1", 6990000m, 7, "ConHang", null, null },
                    { 20, 4, 0, null, null, "Âm thanh vòm 3D sống động.", "/images/giai_tri5.jpg", false, false, "Loa Soundbar LG SN6Y", 3990000m, 9, "ConHang", null, null },
                    { 21, 5, 0, null, null, "Cạo êm, bảo vệ da, pin 50 phút.", "/images/chamsoc1.jpg", false, true, "Máy cạo râu Philips Series 5000", 1599000m, 10, "ConHang", null, null },
                    { 22, 5, 0, null, null, "Sấy nhanh, nhiệt độ ổn định, bảo vệ tóc.", "/images/chamsoc2.jpg", false, false, "Máy sấy tóc Panasonic EH-NA65", 1299000m, 12, "ConHang", null, null },
                    { 23, 5, 0, null, null, "Thiết bị massage đa năng giúp thư giãn cơ bắp, giảm mệt mỏi và hỗ trợ lưu thông máu, phù hợp sử dụng tại nhà sau ngày làm việc căng thẳng.", "/images/chamsoc3.jpg", false, true, "Máy Massage Cầm Tay Beurer MG21", 1299000m, 15, "ConHang", null, null },
                    { 24, 5, 0, null, null, "Làm sạch sâu, chống lão hóa, pin lâu dài.", "/images/chamsoc4.jpg", false, false, "Máy rửa mặt Foreo Luna Mini 3", 2499000m, 8, "ConHang", null, null },
                    { 25, 5, 0, null, null, "Cạo nhanh, an toàn, dễ vệ sinh.", "/images/chamsoc5.jpg", false, true, "Máy cắt tóc Philips HC3505", 899000m, 10, "ConHang", null, null }
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

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ParentReviewId",
                table: "Reviews",
                column: "ParentReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");
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
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Rentals");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vounchers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
