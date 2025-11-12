using Backend.Models;

namespace Backend.Helpers
{
    public static class VoucherCalculator
    {
        public class CalculationResult
        {
            public decimal SubtotalAmount { get; set; }
            public decimal ShippingFee { get; set; }
            public decimal SubtotalDiscount { get; set; }
            public decimal ShippingDiscount { get; set; }
            public decimal TotalDiscount => SubtotalDiscount + ShippingDiscount;
            public decimal FinalAmount => SubtotalAmount + ShippingFee - TotalDiscount;
        }

        /// <summary>
        /// ✅ Tính toán đầy đủ cho voucher (dùng trong controller)
        /// </summary>
        public static CalculationResult Calculate(
            Vouncher voucher,
            decimal subtotal,
            decimal shippingFee)
        {
            var discount = CalcDiscount(voucher, subtotal, shippingFee);

            return new CalculationResult
            {
                SubtotalAmount = subtotal,
                ShippingFee = shippingFee,
                SubtotalDiscount = discount.SubtotalDiscount,
                ShippingDiscount = discount.ShippingDiscount
            };
        }

        public class DiscountResult
        {
            public decimal SubtotalDiscount { get; set; }
            public decimal ShippingDiscount { get; set; }
            public decimal TotalDiscount => SubtotalDiscount + ShippingDiscount;
        }

        /// <summary>
        /// Tính toán discount cho cả subtotal và shipping
        /// </summary>
        public static DiscountResult CalcDiscount(
            Vouncher voucher,
            decimal subtotal,
            decimal shippingFee = 0)
        {
            var result = new DiscountResult();

            if (string.IsNullOrWhiteSpace(voucher.Type))
                return result;

            switch (voucher.Type.ToLowerInvariant())
            {
                case "fixed":
                    // Giảm trực tiếp vào subtotal
                    result.SubtotalDiscount = voucher.DiscountValue ?? 0;
                    break;

                case "percent":
                    // Giảm % subtotal, có giới hạn tối đa
                    var percentDiscount = subtotal * (voucher.DiscountPercent ?? 0) / 100m;
                    result.SubtotalDiscount = voucher.MaximumDiscount.HasValue
                        ? Math.Min(percentDiscount, voucher.MaximumDiscount.Value)
                        : percentDiscount;
                    break;

                case "shipping":
                    // Chỉ giảm phí ship
                    if (voucher.ShippingDiscountPercent.HasValue)
                    {
                        // Giảm % phí ship
                        result.ShippingDiscount = shippingFee * voucher.ShippingDiscountPercent.Value / 100m;
                    }
                    else
                    {
                        // Miễn phí ship 100%
                        result.ShippingDiscount = shippingFee;
                    }
                    break;
            }

            // Đảm bảo không giảm quá số tiền có
            result.SubtotalDiscount = Math.Min(result.SubtotalDiscount, subtotal);
            result.ShippingDiscount = Math.Min(result.ShippingDiscount, shippingFee);

            return result;
        }

        /// <summary>
        /// Backward compatible - chỉ tính discount subtotal
        /// </summary>
        [Obsolete("Sử dụng CalcDiscount(voucher, subtotal, shippingFee) hoặc Calculate() thay thế")]
        public static decimal CalcDiscount(Vouncher voucher, decimal subtotal)
        {
            return CalcDiscount(voucher, subtotal, 0).SubtotalDiscount;
        }
    }
}
