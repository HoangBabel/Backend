using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Vouncher> Vounchers { get; set; }

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
