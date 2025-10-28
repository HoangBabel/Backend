using Backend.Models;

namespace Backend.Helpers
{
    public class VoucherValidator
    {
        public static bool IsUsable(Vouncher v, decimal subtotal)
        {
            if (v == null) return false;
            if (!v.IsValid) return false;
            if (v.ExpirationDate < DateTime.UtcNow) return false;
            if (subtotal < v.MinimumOrderValue) return false;
            if (v.MaxUsageCount > 0 && v.CurrentUsageCount >= v.MaxUsageCount) return false;
            return true;
        }
    }
}
