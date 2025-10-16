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
                     IsActive = true
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
                    IsDeleted = false
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
                    IsDeleted = false
                },
                new Product
                {
                    IdProduct = 3,
                    CategoryId = 2,
                    Name = "Tủ lạnh Panasonic Inverter 322 lít NR-BV361BPKV",
                    Price = 12490000m,
                    Quantity = 8,
                    Description = "Công nghệ tiết kiệm điện Inverter, ngăn đông mềm Prime Fresh+.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false
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
                    IsDeleted = false
                },
                new Product
                {
                    IdProduct = 5,
                    CategoryId = 3,
                    Name = "Máy giặt Electrolux Inverter 10kg EWF1024BDWA",
                    Price = 8990000m,
                    Quantity = 10,
                    Description = "Công nghệ UltraMix hòa tan bột giặt, giảm phai màu quần áo.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false
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
                    IsDeleted = false
                },
                new Product
                {
                    IdProduct = 7,
                    CategoryId = 4,
                    Name = "Máy lạnh Daikin Inverter 1.5 HP FTKY35WMVMV",
                    Price = 11490000m,
                    Quantity = 9,
                    Description = "Công nghệ Streamer khử khuẩn, tiết kiệm điện vượt trội.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false
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
                    IsDeleted = false
                },
                new Product
                {
                    IdProduct = 9,
                    CategoryId = 5,
                    Name = "Nồi chiên không dầu Philips HD9200/90 4.1L",
                    Price = 2290000m,
                    Quantity = 15,
                    Description = "Công nghệ Rapid Air giảm 90% dầu mỡ, vỏ thép sơn tĩnh điện.",
                    Status = ProductStatus.ConHang,
                    IsDeleted = false
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
                    IsDeleted = false
                }
            );
        }
    }
}
