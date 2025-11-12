using Backend.Models;

namespace Backend.Helpers
{
    public static class VoucherValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// ✅ Kiểm tra type hợp lệ
        /// </summary>
        public static bool IsValidVoucherType(string type)
        {
            var normalized = type?.ToLowerInvariant();
            return normalized is "fixed" or "percent" or "shipping";
        }

        /// <summary>
        /// ✅ Validate các field của voucher theo type
        /// </summary>
        public static bool ValidateVoucherFields(Vouncher voucher, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(voucher.Type))
            {
                errorMessage = "Type không được để trống";
                return false;
            }

            var type = voucher.Type.ToLowerInvariant();

            switch (type)
            {
                case "fixed":
                    if (!voucher.DiscountValue.HasValue || voucher.DiscountValue.Value <= 0)
                    {
                        errorMessage = "Voucher Fixed phải có DiscountValue > 0";
                        return false;
                    }
                    break;

                case "percent":
                    if (!voucher.DiscountPercent.HasValue || voucher.DiscountPercent.Value <= 0)
                    {
                        errorMessage = "Voucher Percent phải có DiscountPercent > 0";
                        return false;
                    }
                    if (voucher.DiscountPercent.Value > 100)
                    {
                        errorMessage = "DiscountPercent không được vượt quá 100%";
                        return false;
                    }
                    break;

                case "shipping":
                    if (voucher.ShippingDiscountPercent.HasValue)
                    {
                        if (voucher.ShippingDiscountPercent.Value < 0 ||
                            voucher.ShippingDiscountPercent.Value > 100)
                        {
                            errorMessage = "ShippingDiscountPercent phải từ 0-100%";
                            return false;
                        }
                    }
                    break;

                default:
                    errorMessage = $"Loại voucher '{voucher.Type}' không hợp lệ";
                    return false;
            }

            return true;
        }

        /// <summary>
        /// ✅ Kiểm tra voucher có khả dụng không (overload với out error)
        /// </summary>
        public static bool IsUsable(Vouncher voucher, decimal subtotal, out string errorMessage)
        {
            var result = Validate(voucher, subtotal);
            errorMessage = result.ErrorMessage;
            return result.IsValid;
        }

        /// <summary>
        /// Kiểm tra voucher có khả dụng không (backward compatible)
        /// </summary>
        public static bool IsUsable(Vouncher voucher, decimal subtotal)
        {
            return Validate(voucher, subtotal).IsValid;
        }

        /// <summary>
        /// Validate chi tiết voucher
        /// </summary>
        public static ValidationResult Validate(Vouncher voucher, decimal subtotal)
        {
            // 1. Kiểm tra IsValid flag
            if (!voucher.IsValid)
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher không còn hiệu lực"
                };

            // 2. Kiểm tra hết hạn
            if (voucher.ExpirationDate < DateTime.UtcNow)
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher đã hết hạn"
                };

            // 3. Kiểm tra số lần sử dụng
            if (voucher.MaxUsageCount > 0 &&
                voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher đã hết lượt sử dụng"
                };

            // 4. Kiểm tra giá trị đơn hàng tối thiểu
            if (subtotal < voucher.MinimumOrderValue)
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Đơn hàng phải tối thiểu {voucher.MinimumOrderValue:N0}đ để sử dụng voucher này"
                };

            // 5. Validate fields theo type
            if (!ValidateVoucherFields(voucher, out string fieldError))
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = fieldError
                };

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// ✅ Lấy số lượt sử dụng còn lại
        /// </summary>
        public static int GetRemainingUsage(Vouncher voucher)
        {
            if (voucher.MaxUsageCount <= 0)
                return int.MaxValue; // Unlimited

            var remaining = voucher.MaxUsageCount - voucher.CurrentUsageCount;
            return Math.Max(0, remaining);
        }
    }
}
