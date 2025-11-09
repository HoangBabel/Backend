using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Vouncher> Vounchers { get; set; } = default!;
        public DbSet<Cart> Carts { get; set; } = default!;
        public DbSet<CartItem> CartItems { get; set; } = default!; // ← Lớp số ít
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalItem> RentalItems { get; set; }
        public DbSet<RentalPlan> RentalPlans => Set<RentalPlan>();
        public DbSet<RentalPricingTier> RentalPricingTiers => Set<RentalPricingTier>();


        public override int SaveChanges()
        {
            ApplyProductRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            ApplyProductRules();
            return base.SaveChangesAsync(ct);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User 1—N Cart
            modelBuilder.Entity<User>()
                .HasMany(u => u.Carts)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart 1—N CartItem
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem N—1 Product
            modelBuilder.Entity<CartItem>()
                .HasOne(i => i.Product)
                .WithMany(p => p.CartItems!)    // ← cần nav ngược trong Product
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
               .HasMany(o => o.Items)
               .WithOne(oi => oi.Order)
               .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique()
                .HasFilter("[IsCheckedOut] = 0")
                .HasDatabaseName("IX_Carts_UserId_NotCheckedOut");
            // ===== CẤU HÌNH THUÊ THEO NGÀY =====

            // 🔹 RentalPlan: (ProductId, Unit) duy nhất
            modelBuilder.Entity<RentalPlan>()
                .HasIndex(x => new { x.ProductId, x.Unit })
                .IsUnique();

            // 🔹 RentalPricingTier: (ProductId, ThresholdDays) duy nhất
            modelBuilder.Entity<RentalPricingTier>()
                .HasIndex(x => new { x.ProductId, x.ThresholdDays })
                .IsUnique();

            // 🔹 Quan hệ 1-n giữa Rental và RentalItem
            modelBuilder.Entity<RentalItem>()
                .HasOne(ri => ri.Rental)
                .WithMany(r => r.Items)
                .HasForeignKey(ri => ri.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Precision cho các cột decimal
            modelBuilder.Entity<Rental>()
                .Property(r => r.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Rental>()
                .Property(r => r.DepositPaid).HasPrecision(18, 2);
            modelBuilder.Entity<Rental>()
                .Property(r => r.LateFee).HasPrecision(18, 2);
            modelBuilder.Entity<Rental>()
                .Property(r => r.CleaningFee).HasPrecision(18, 2);
            modelBuilder.Entity<Rental>()
                .Property(r => r.DamageFee).HasPrecision(18, 2);
            modelBuilder.Entity<Rental>()
                .Property(r => r.DepositRefund).HasPrecision(18, 2);

            modelBuilder.Entity<RentalItem>()
                .Property(ri => ri.PricePerUnitAtBooking).HasPrecision(18, 2);
            modelBuilder.Entity<RentalItem>()
                .Property(ri => ri.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<RentalItem>()
                .Property(ri => ri.DepositAtBooking).HasPrecision(18, 2);
            modelBuilder.Entity<RentalItem>()
                .Property(ri => ri.LateFeePerUnitAtBooking).HasPrecision(18, 2);

            // Gọi hàm seed dữ liệu
            ProductSeed.Seed(modelBuilder);
        }

        private void ApplyProductRules()
        {
            foreach (var entry in ChangeTracker.Entries<Product>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    if (entry.Entity.Quantity < 0)
                        throw new ValidationException("Số lượng không được âm.");

                    entry.Entity.Status = entry.Entity.Quantity > 0
                        ? ProductStatus.ConHang
                        : ProductStatus.HetHang;
                }
            }
        }
    }
}
