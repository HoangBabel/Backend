using Backend.Models;

namespace Backend.Helpers
{
    public static class RentalPricingHelper
    {
        /// Tính số ngày làm tròn lên (End > Start)
        public static int ComputeDays(DateTime start, DateTime end)
        {
            if (end <= start) throw new ArgumentException("EndDate phải lớn hơn StartDate.");
            return (int)Math.Ceiling((end - start).TotalDays);
        }

        /// Chọn bậc giá có ThresholdDays lớn nhất nhưng <= days; nếu không có tier phù hợp → dùng basePrice
        public static (decimal pricePerDay, int appliedThreshold) PickTierPriceOrBase(
            int days,
            IEnumerable<RentalPricingTier> tiers,
            decimal basePricePerDay)
        {
            var tier = tiers
                .Where(t => t.ThresholdDays <= days)
                .OrderByDescending(t => t.ThresholdDays)
                .FirstOrDefault();

            return tier is null
                ? (basePricePerDay, 0)
                : (tier.PricePerDay, tier.ThresholdDays);
        }
    }

}
