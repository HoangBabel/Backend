using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers
{
    public static class RentalPlanAutoGenerator
    {
        /// Tạo RentalPlan + Tiers mặc định dựa trên giá bán Product.Price
        public static async Task<(RentalPlan plan, List<RentalPricingTier> tiers)> EnsureDailyPlanAsync(
            AppDbContext db, int productId)
        {
            // 1) Nếu đã có -> trả về luôn
            var existing = await db.RentalPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.Unit == RentalUnit.Day);

            if (existing != null)
            {
                var existingTiers = await db.RentalPricingTiers
                    .AsNoTracking()
                    .Where(t => t.ProductId == productId)
                    .OrderBy(t => t.ThresholdDays)
                    .ToListAsync();

                return (existing, existingTiers);
            }

            // 2) Lấy product & tính price/day + deposit
            var product = await db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProduct == productId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

            if (!product.IsRental)
                throw new InvalidOperationException("Sản phẩm này không được bật cho thuê.");

            // Công thức chuẩn: base/day ≈ Price / (18*30*0.60) * 1.15
            decimal price = product.Price;
            decimal basePerDayRaw = price / (18m * 30m * 0.60m) * 1.15m;

            // Làm tròn “tâm lý” về bội số 1000
            decimal BaseRound(decimal v)
            {
                var k = Math.Round(v / 1000m, MidpointRounding.AwayFromZero) * 1000m;
                return Math.Max(1000m, k); // tối thiểu 1k/ngày cho an toàn
            }

            var basePerDay = BaseRound(basePerDayRaw);            // ví dụ 25,000đ
            var deposit = BaseRound(price * 0.70m);            // cọc 70%
            var latePerDay = BaseRound(basePerDay * 1.20m);       // phí trễ ~120%

            var plan = new RentalPlan
            {
                ProductId = productId,
                Unit = RentalUnit.Day,
                PricePerUnit = basePerDay,
                MinUnits = 2,
                Deposit = deposit,
                LateFeePerUnit = latePerDay
            };
            db.RentalPlans.Add(plan);

            // 3) Tiers (nếu bạn dùng): 1–3: +20%, 4–7: +8%, 8–14: base, 15–29: −12%, ≥30: −24%
            decimal T(decimal v) => BaseRound(v);

            var tiers = new List<RentalPricingTier>
        {
            new() { ProductId = productId, ThresholdDays = 1,  PricePerDay = T(basePerDay * 1.20m) }, // 1–3
            new() { ProductId = productId, ThresholdDays = 4,  PricePerDay = T(basePerDay * 1.08m) }, // 4–7
            new() { ProductId = productId, ThresholdDays = 8,  PricePerDay = T(basePerDay * 1.00m) }, // 8–14
            new() { ProductId = productId, ThresholdDays = 15, PricePerDay = T(basePerDay * 0.88m) }, // 15–29
            new() { ProductId = productId, ThresholdDays = 30, PricePerDay = T(basePerDay * 0.76m) }  // ≥30
        };

            db.RentalPricingTiers.AddRange(tiers);
            await db.SaveChangesAsync();

            return (plan, tiers);
        }
    }
}
