using Backend.Models;

namespace Backend.Helpers
{
    public static class VoucherCalculator
    {
        public static decimal CalcDiscount(Vouncher v, decimal subtotal)
        {
            if (v == null) return 0m;

            string type = v.Type.ToString().ToLowerInvariant();
            decimal discount = 0m;

            if (type == "fixed")
            {
                var val = (v.DiscountValue ?? 0);
                discount = Math.Clamp((decimal)val, 0m, subtotal);
            }
            else if (type == "percent")
            {
                var pct = Math.Clamp(v.DiscountPercent ?? 0, 0, 100);
                var raw = subtotal * (pct / 100m);
                if (v.MaximumDiscount.HasValue)
                    raw = Math.Min(raw, v.MaximumDiscount.Value);
                discount = Math.Clamp(raw, 0m, subtotal);
            }
            else if (type == "shipping")
            {
                // Nếu không dùng shippingFee thì không giảm gì
                discount = 0m;
            }

            return discount;
        }

    }
}

