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
                    Id = 1,
                    Username = "admin",
                    Email = "hoangphap1000@gmail.com",
                    PasswordHash = "$2a$11$5WvpePUu2EIg8jo7MBWjvee3/uwro4V6QUIRSAju3HSEJVmvwcXJe", //123456
                    FullName = "Lê Hoàng Pháp",
                    PhoneNumber = "0564090866",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.Parse("2025-11-11T15:27:39.7072879Z"),
                    IsActive = true,
                    IsTwoFactorEnabled = false,
                    AvatarUrl = "https://localhost:44303/uploads/avatars/d580bd4e-3fce-4964-9e0c-53177f34082c.png"
                },
                new User
                {
                    Id = 2,
                    Username = "giang",
                    Email = "giang@example.com",
                    PasswordHash = "$2a$11$zB0cPctNLMkRJqNbC7qc7eF.VvtXVr1KmCuGUEoXC331zdp4Q9J.a", //Giang@123
                    FullName = "Le Giang",
                    PhoneNumber = "0773678161",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.Parse("2025-10-16T03:32:39.9245745Z"),
                    IsActive = true,
                    IsTwoFactorEnabled = false
                });

            // --- CATEGORY ---
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Chế biến thực phẩm" },
                new Category { CategoryId = 2, Name = "Làm mát và giữ lạnh" },
                new Category { CategoryId = 3, Name = "Giặt giũ và vệ sinh" },
                new Category { CategoryId = 4, Name = "Giải trí" },
                new Category { CategoryId = 5, Name = "Chăm sóc cá nhân" }
            );

            // --- PRODUCT ---
            modelBuilder.Entity<Product>().HasData(
    // --- Nấu ăn và chế biến thực phẩm ---
    new Product { IdProduct = 1, CategoryId = 1, Name = "Nồi cơm điện Cuckoo CR-0675F", Price = 1590000m, Quantity = 18, Description = "Nấu cơm nhanh, giữ ấm lâu, dễ vệ sinh.", Image = "/images/giadung1.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 2, CategoryId = 1, Name = "Nồi chiên không dầu Philips HD9200/90", Price = 2290000m, Quantity = 15, Description = "Công nghệ Rapid Air giảm 90% dầu mỡ.", Image = "/images/giadung2.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 3, CategoryId = 1, Name = "Lò vi sóng Sharp R-G226VN-BK", Price = 1990000m, Quantity = 14, Description = "Hâm, nấu, rã đông nhanh, núm xoay cơ học.", Image = "/images/giadung3.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 4, CategoryId = 1, Name = "Máy xay sinh tố Philips HR2221/00", Price = 1250000m, Quantity = 12, Description = "Công suất 700W, lưỡi dao thép không gỉ.", Image = "/images/giadung4.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 5, CategoryId = 1, Name = "Máy pha cà phê Delonghi EC685", Price = 3990000m, Quantity = 10, Description = "Pha espresso chất lượng, thiết kế nhỏ gọn.", Image = "/images/giadung5.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },

    // --- Làm mát và giữ lạnh ---
    new Product { IdProduct = 6, CategoryId = 2, Name = "Tủ lạnh Samsung Inverter 424 lít RT42CG6324B1SV", Price = 14990000m, Quantity = 7, Description = "Làm lạnh nhanh, khử mùi than hoạt tính.", Image = "/images/lammat1.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 7, CategoryId = 2, Name = "Tủ đông Electrolux 200 lít EFZ2200H-H", Price = 8990000m, Quantity = 8, Description = "Bảo quản thực phẩm lâu dài, tiết kiệm điện.", Image = "/images/lammat2.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 8, CategoryId = 2, Name = "Quạt điều hòa Midea AC120-19AR", Price = 3290000m, Quantity = 12, Description = "Làm mát nhanh, lọc bụi, 3 chế độ gió.", Image = "/images/lammat3.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 9, CategoryId = 2, Name = "Máy điều hòa Daikin Inverter 1.5 HP FTKY35WMVMV", Price = 11490000m, Quantity = 9, Description = "Khử khuẩn, tiết kiệm điện vượt trội.", Image = "/images/lammat4.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 10, CategoryId = 2, Name = "Quạt điện Asia F16001", Price = 550000m, Quantity = 20, Description = "Tiện dụng, vận hành êm, thiết kế nhỏ gọn.", Image = "/images/lammat5.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },

    // --- Giặt giũ và vệ sinh ---
    new Product { IdProduct = 11, CategoryId = 3, Name = "Máy giặt Electrolux Inverter 10kg EWF1024BDWA", Price = 8990000m, Quantity = 10, Description = "Công nghệ UltraMix hòa tan bột giặt.", Image = "/images/giatgiu1.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 12, CategoryId = 3, Name = "Máy rửa chén Bosch SMS25CI00E", Price = 12990000m, Quantity = 6, Description = "Rửa sạch, tiết kiệm nước, vận hành êm ái.", Image = "/images/giatgiu2.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 13, CategoryId = 3, Name = "Robot hút bụi Xiaomi Mi Robot Vacuum", Price = 4990000m, Quantity = 15, Description = "Hút bụi tự động, lập bản đồ thông minh.", Image = "/images/giatgiu3.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 14, CategoryId = 3, Name = "Máy lọc không khí Sharp FP-J40E-B", Price = 3990000m, Quantity = 8, Description = "Loại bỏ bụi mịn, vi khuẩn và mùi hôi.", Image = "/images/giatgiu4.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 15, CategoryId = 3, Name = "Máy sấy quần áo Electrolux EDV705HQWA", Price = 5990000m, Quantity = 9, Description = "Sấy nhanh, tiết kiệm điện, bảo vệ vải.", Image = "/images/giatgiu5.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },

    // --- Giải trí ---
    new Product { IdProduct = 16, CategoryId = 4, Name = "Tivi Samsung Crystal UHD 55 inch BU8000", Price = 12990000m, Quantity = 12, Description = "Smart Tivi 4K sắc nét, hỗ trợ giọng nói.", Image = "/images/giai_tri1.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 17, CategoryId = 4, Name = "Loa Bluetooth JBL Charge 5", Price = 2590000m, Quantity = 15, Description = "Âm thanh mạnh mẽ, pin 20 giờ.", Image = "/images/giai_tri2.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 18, CategoryId = 4, Name = "Tivi LG 4K Smart TV 55 inch UQ7550", Price = 11990000m, Quantity = 10, Description = "Màn hình 4K sắc nét, hệ điều hành webOS, hỗ trợ điều khiển giọng nói.", Image = "/images/giai_tri3.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 19, CategoryId = 4, Name = "Máy chiếu mini ViewSonic M1", Price = 6990000m, Quantity = 7, Description = "Chiếu phim Full HD, pin tích hợp.", Image = "/images/giai_tri4.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 20, CategoryId = 4, Name = "Loa Soundbar LG SN6Y", Price = 3990000m, Quantity = 9, Description = "Âm thanh vòm 3D sống động.", Image = "/images/giai_tri5.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },

    // --- Chăm sóc cá nhân ---
    new Product { IdProduct = 21, CategoryId = 5, Name = "Máy cạo râu Philips Series 5000", Price = 1599000m, Quantity = 10, Description = "Cạo êm, bảo vệ da, pin 50 phút.", Image = "/images/chamsoc1.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 22, CategoryId = 5, Name = "Máy sấy tóc Panasonic EH-NA65", Price = 1299000m, Quantity = 12, Description = "Sấy nhanh, nhiệt độ ổn định, bảo vệ tóc.", Image = "/images/chamsoc2.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 23, CategoryId = 5, Name = "Máy Massage Cầm Tay Beurer MG21", Price = 1299000m, Quantity = 15, Description = "Thiết bị massage đa năng giúp thư giãn cơ bắp, giảm mệt mỏi và hỗ trợ lưu thông máu, phù hợp sử dụng tại nhà sau ngày làm việc căng thẳng.", Image = "/images/chamsoc3.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New },
    new Product { IdProduct = 24, CategoryId = 5, Name = "Máy rửa mặt Foreo Luna Mini 3", Price = 2499000m, Quantity = 8, Description = "Làm sạch sâu, chống lão hóa, pin lâu dài.", Image = "/images/chamsoc4.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = false, Condition = ProductCondition.New },
    new Product { IdProduct = 25, CategoryId = 5, Name = "Máy cắt tóc Philips HC3505", Price = 899000m, Quantity = 10, Description = "Cạo nhanh, an toàn, dễ vệ sinh.", Image = "/images/chamsoc5.jpg", Status = ProductStatus.ConHang, IsDeleted = false, IsRental = true, Condition = ProductCondition.New }
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
