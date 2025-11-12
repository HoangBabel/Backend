using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public static class ProductSeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            // --- USER ---
            modelBuilder.Entity<User>().HasData(
                 new User
                 {
                     Id = 10,
                     Username = "giang",
                     Email = "giang@example.com",
                     PasswordHash = "$2a$11$zB0cPctNLMkRJqNbC7qc7eF.VvtXVr1KmCuGUEoXC331zdp4Q9J.a",//giang123@
                     FullName = "le giang",
                     PhoneNumber = "0773678161",
                     Role = UserRole.Admin,
                     CreatedAt = DateTime.Parse("2025-10-16T03:32:39.9245745Z"),
                     IsActive = true,
                     IsTwoFactorEnabled = false 
                 });
            // --- CATEGORY ---
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Tivi" },
                new Category { CategoryId = 2, Name = "Tủ lạnh" },
                new Category { CategoryId = 3, Name = "Máy giặt" },
                new Category { CategoryId = 4, Name = "Máy lạnh" },
                new Category { CategoryId = 5, Name = "Gia dụng nhà bếp" }
            );

            // --- PRODUCT ---
            modelBuilder.Entity<Product>().HasData(
               new Product
               {
                   IdProduct = 1,
                   CategoryId = 1,
                   Name = "Tivi Samsung Crystal UHD 55 inch BU8000",
                   Price = 12990000m,
                   Quantity = 12,
                   Description = "Smart Tivi 4K hiển thị sắc nét, hỗ trợ điều khiển bằng giọng nói.",
                   Status = ProductStatus.ConHang,
                   IsDeleted = false,
                   IsRental = false,
                   Condition = ProductCondition.New
               },
                new Product
                {
                    IdProduct = 2,
                    CategoryId = 1,
                    Name = "Tivi LG OLED evo 48 inch C3",
                    Price = 27990000m,
                    Quantity = 5,
                    Description = "Màn hình OLED siêu mỏng, hỗ trợ Dolby Vision và Dolby Atmos.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },

                // TỦ LẠNH - Sản phẩm bán
                new Product
                {
                    IdProduct = 3,
                    CategoryId = 2,
                    Name = "Tủ lạnh Panasonic Inverter 322 lít NR-BV361BPKV",
                    Price = 12490000m,
                    Quantity = 8,
                    Description = "Công nghệ tiết kiệm điện Inverter, ngăn đông mềm Prime Fresh+.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },
                new Product
                {
                    IdProduct = 4,
                    CategoryId = 2,
                    Name = "Tủ lạnh Samsung Inverter 424 lít RT42CG6324B1SV",
                    Price = 14990000m,
                    Quantity = 7,
                    Description = "Làm lạnh nhanh, khử mùi bằng than hoạt tính, thiết kế sang trọng.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },

                // MÁY GIẶT - Sản phẩm bán
                new Product
                {
                    IdProduct = 5,
                    CategoryId = 3,
                    Name = "Máy giặt Electrolux Inverter 10kg EWF1024BDWA",
                    Price = 8990000m,
                    Quantity = 10,
                    Description = "Công nghệ UltraMix hòa tan bột giặt, giảm phai màu quần áo.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },
                new Product
                {
                    IdProduct = 6,
                    CategoryId = 3,
                    Name = "Máy giặt Samsung Inverter 9.5kg WW95T504DAW/SV",
                    Price = 8790000m,
                    Quantity = 6,
                    Description = "AI Control tự động tối ưu chương trình giặt, tiết kiệm năng lượng.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },

                // MÁY LẠNH - Sản phẩm bán
                new Product
                {
                    IdProduct = 7,
                    CategoryId = 4,
                    Name = "Máy lạnh Daikin Inverter 1.5 HP FTKY35WMVMV",
                    Price = 11490000m,
                    Quantity = 9,
                    Description = "Công nghệ Streamer khử khuẩn, tiết kiệm điện vượt trội.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },
                new Product
                {
                    IdProduct = 8,
                    CategoryId = 4,
                    Name = "Máy lạnh LG Inverter 1 HP V10WIN",
                    Price = 8390000m,
                    Quantity = 11,
                    Description = "Làm lạnh nhanh, kháng khuẩn bằng ion bạc, vận hành êm ái.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },

                // GIA DỤNG NHÀ BẾP - Sản phẩm bán
                new Product
                {
                    IdProduct = 9,
                    CategoryId = 5,
                    Name = "Nồi chiên không dầu Philips HD9200/90 4.1L",
                    Price = 2290000m,
                    Quantity = 15,
                    Description = "Công nghệ Rapid Air giảm 90% dầu mỡ, vỏ thép sơn tĩnh điện.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },
                new Product
                {
                    IdProduct = 10,
                    CategoryId = 5,
                    Name = "Lò vi sóng Sharp R-G226VN-BK 20 lít",
                    Price = 1990000m,
                    Quantity = 14,
                    Description = "Chức năng hâm, nấu, rã đông nhanh, điều khiển núm xoay cơ học.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = false,
                    Condition = ProductCondition.New
                },

                // --- SẢN PHẨM CHO THUÊ ---

                // TIVI cho thuê
                new Product
                {
                    IdProduct = 11,
                    CategoryId = 1,
                    Name = "Tivi Samsung 43 inch (Cho thuê)",
                    Price = 500000m, // giá thuê/tháng
                    Quantity = 20,
                    Description = "Smart Tivi Full HD 43 inch cho thuê theo tháng, phù hợp sự kiện, văn phòng.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },
                new Product
                {
                    IdProduct = 12,
                    CategoryId = 1,
                    Name = "Tivi LG 55 inch 4K (Cho thuê)",
                    Price = 800000m,
                    Quantity = 15,
                    Description = "Smart Tivi 4K 55 inch cho thuê, hỗ trợ lắp đặt tận nơi.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },

                // TỦ LẠNH cho thuê
                new Product
                {
                    IdProduct = 13,
                    CategoryId = 2,
                    Name = "Tủ lạnh 180 lít (Cho thuê)",
                    Price = 400000m,
                    Quantity = 25,
                    Description = "Tủ lạnh mini cho thuê theo tháng, phù hợp phòng trọ, sinh viên.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },
                new Product
                {
                    IdProduct = 14,
                    CategoryId = 2,
                    Name = "Tủ lạnh Inverter 350 lít (Cho thuê)",
                    Price = 700000m,
                    Quantity = 12,
                    Description = "Tủ lạnh Inverter tiết kiệm điện cho thuê, bảo hành trong thời gian thuê.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },

                // MÁY GIẶT cho thuê
                new Product
                {
                    IdProduct = 15,
                    CategoryId = 3,
                    Name = "Máy giặt 8kg (Cho thuê)",
                    Price = 450000m,
                    Quantity = 18,
                    Description = "Máy giặt cửa trên 8kg cho thuê, phù hợp gia đình nhỏ.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },
                new Product
                {
                    IdProduct = 16,
                    CategoryId = 3,
                    Name = "Máy giặt Inverter 9kg (Cho thuê)",
                    Price = 600000m,
                    Quantity = 10,
                    Description = "Máy giặt Inverter tiết kiệm điện, vận hành êm ái cho thuê theo tháng.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },

                // MÁY LẠNH cho thuê
                new Product
                {
                    IdProduct = 17,
                    CategoryId = 4,
                    Name = "Máy lạnh 1 HP (Cho thuê)",
                    Price = 550000m,
                    Quantity = 30,
                    Description = "Máy lạnh 1 HP cho thuê theo tháng, bảo trì miễn phí.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },
                new Product
                {
                    IdProduct = 18,
                    CategoryId = 4,
                    Name = "Máy lạnh Inverter 1.5 HP (Cho thuê)",
                    Price = 750000m,
                    Quantity = 22,
                    Description = "Máy lạnh Inverter tiết kiệm điện, làm lạnh nhanh cho thuê.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },

                // GIA DỤNG NHÀ BẾP cho thuê
                new Product
                {
                    IdProduct = 19,
                    CategoryId = 5,
                    Name = "Lò vi sóng 20L (Cho thuê)",
                    Price = 200000m,
                    Quantity = 25,
                    Description = "Lò vi sóng cho thuê theo tháng, phù hợp văn phòng, phòng trọ.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                },
                new Product
                {
                    IdProduct = 20,
                    CategoryId = 5,
                    Name = "Nồi chiên không dầu 5L (Cho thuê)",
                    Price = 25000m,
                    Quantity = 20,
                    Description = "Nồi chiên không dầu dung tích lớn cho thuê, phù hợp gia đình.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false,
                    IsRental = true,
                    Condition = ProductCondition.Used
                }
            );
            modelBuilder.Entity<Vouncher>().HasData(
// ✅ 1. Voucher Fixed - Giảm cố định
                new Vouncher
                {
                    Id = 1,
                    Code = "GIAM500K",
                    Type = "fixed", // ✅ Viết thường
                    DiscountValue = 500000m,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 5000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅ UTC
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 100,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 2,
                    Code = "GIAM1M",
                    Type = "fixed", // ✅
                    DiscountValue = 1000000m,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 10000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 50,
                    CurrentUsageCount = 0
                },

                // ✅ 2. Voucher Percent - Giảm %
                new Vouncher
                {
                    Id = 3,
                    Code = "GIAM10",
                    Type = "percent", // ✅
                    DiscountValue = null,
                    DiscountPercent = 10m,
                    MaximumDiscount = 500000m,
                    MinimumOrderValue = 2000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 200,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 4,
                    Code = "GIAM20",
                    Type = "percent", // ✅
                    DiscountValue = null,
                    DiscountPercent = 20m,
                    MaximumDiscount = 1000000m,
                    MinimumOrderValue = 5000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 100,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 5,
                    Code = "GIAM30",
                    Type = "percent", // ✅
                    DiscountValue = null,
                    DiscountPercent = 30m,
                    MaximumDiscount = 2000000m,
                    MinimumOrderValue = 10000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 30,
                    CurrentUsageCount = 0
                },

                // ✅ 3. Voucher Shipping - Miễn phí ship
                new Vouncher
                {
                    Id = 6,
                    Code = "FREESHIP",
                    Type = "shipping", // ✅
                    DiscountValue = null,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 1000000m,
                    ApplyToShipping = true,
                    ShippingDiscountPercent = null, // null = 100% freeship
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 500,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 7,
                    Code = "SHIP50",
                    Type = "shipping", // ✅
                    DiscountValue = null,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 500000m,
                    ApplyToShipping = true,
                    ShippingDiscountPercent = 50m, // Giảm 50% ship
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 300,
                    CurrentUsageCount = 0
                },

                // ❌ 4. VOUCHER COMBO - KHÔNG HỢP LỆ - CẦN XÓA HOẶC SỬA
                // Giải pháp 1: Xóa voucher này
                // Giải pháp 2: Tách thành 2 voucher riêng
                // Giải pháp 3: Sửa logic code để hỗ trợ combo

                // ✅ Thay thế bằng 2 voucher riêng:
                new Vouncher
                {
                    Id = 8,
                    Code = "COMBO200K", // Giảm sản phẩm
                    Type = "fixed", // ✅
                    DiscountValue = 200000m,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 3000000m,
                    ApplyToShipping = false, // ✅ Fixed không giảm ship
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 150,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 13, // Thêm voucher mới
                    Code = "COMBOSHIP", // Miễn phí ship kèm
                    Type = "shipping", // ✅
                    DiscountValue = null,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 3000000m,
                    ApplyToShipping = true,
                    ShippingDiscountPercent = null, // 100% freeship
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 150,
                    CurrentUsageCount = 0
                },

                // ✅ 5. Voucher cho thuê
                new Vouncher
                {
                    Id = 9,
                    Code = "THUE100K",
                    Type = "fixed", // ✅
                    DiscountValue = 100000m,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 1000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 200,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 10,
                    Code = "THUE15",
                    Type = "percent", // ✅
                    DiscountValue = null,
                    DiscountPercent = 15m,
                    MaximumDiscount = 300000m,
                    MinimumOrderValue = 2000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 100,
                    CurrentUsageCount = 0
                },

                // ✅ 6. Test Cases
                new Vouncher
                {
                    Id = 11,
                    Code = "EXPIRED",
                    Type = "fixed", // ✅
                    DiscountValue = 500000m,
                    DiscountPercent = null,
                    MaximumDiscount = null,
                    MinimumOrderValue = 1000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), // ✅ Đã hết hạn
                    IsValid = true, // ✅ Để validator tự phát hiện
                    UsedAt = null,
                    MaxUsageCount = 10,
                    CurrentUsageCount = 0
                },
                new Vouncher
                {
                    Id = 12,
                    Code = "SOLDOUT",
                    Type = "percent", // ✅
                    DiscountValue = null,
                    DiscountPercent = 25m,
                    MaximumDiscount = 1000000m,
                    MinimumOrderValue = 3000000m,
                    ApplyToShipping = false,
                    ShippingDiscountPercent = null,
                    ExpirationDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), // ✅
                    IsValid = true,
                    UsedAt = null,
                    MaxUsageCount = 5,
                    CurrentUsageCount = 5 // Đã hết lượt
                }

                );
        }
    }
}
