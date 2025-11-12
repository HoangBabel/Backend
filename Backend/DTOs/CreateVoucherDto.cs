using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreateVoucherDto
    {
        [Required(ErrorMessage = "Code là bắt buộc")]
        [StringLength(50, ErrorMessage = "Code tối đa 50 ký tự")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type là bắt buộc")]
        public string Type { get; set; } = string.Empty; // "Fixed", "Percent", "Shipping"

        public decimal? DiscountValue { get; set; }

        [Range(0, 100, ErrorMessage = "DiscountPercent phải từ 0-100")]
        public decimal? DiscountPercent { get; set; }

        public decimal? MaximumDiscount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "MinimumOrderValue phải >= 0")]
        public decimal MinimumOrderValue { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "ShippingDiscountPercent phải từ 0-100")]
        public decimal? ShippingDiscountPercent { get; set; }

        [Required(ErrorMessage = "ExpirationDate là bắt buộc")]
        public DateTime ExpirationDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MaxUsageCount phải >= 1")]
        public int MaxUsageCount { get; set; } = 1;
    }

    public class UpdateVoucherDto
    {
        [StringLength(50, ErrorMessage = "Code tối đa 50 ký tự")]
        public string? Code { get; set; }

        public string? Type { get; set; }

        public decimal? DiscountValue { get; set; }

        [Range(0, 100, ErrorMessage = "DiscountPercent phải từ 0-100")]
        public decimal? DiscountPercent { get; set; }

        public decimal? MaximumDiscount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "MinimumOrderValue phải >= 0")]
        public decimal? MinimumOrderValue { get; set; }

        [Range(0, 100, ErrorMessage = "ShippingDiscountPercent phải từ 0-100")]
        public decimal? ShippingDiscountPercent { get; set; }

        public DateTime? ExpirationDate { get; set; }
        public bool? IsValid { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MaxUsageCount phải >= 1")]
        public int? MaxUsageCount { get; set; }
    }

    public class ValidateVoucherRequest
    {
        [Required(ErrorMessage = "Code là bắt buộc")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "SubtotalAmount là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "SubtotalAmount phải >= 0")]
        public decimal SubtotalAmount { get; set; }

        [Required(ErrorMessage = "ShippingFee là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "ShippingFee phải >= 0")]
        public decimal ShippingFee { get; set; }
    }

    public class ApplyVoucherRequest
    {
        [Required(ErrorMessage = "Code là bắt buộc")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "SubtotalAmount là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "SubtotalAmount phải >= 0")]
        public decimal SubtotalAmount { get; set; }

        [Required(ErrorMessage = "ShippingFee là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "ShippingFee phải >= 0")]
        public decimal ShippingFee { get; set; }
    }

    public class VoucherValidationResponse
    {
        public bool IsValid { get; set; }
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal SubtotalDiscount { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalAmount { get; set; }
        public int RemainingUsage { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VoucherApplicationResponse
    {
        public bool Success { get; set; }
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal SubtotalDiscount { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalAmount { get; set; }
        public int RemainingUsage { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
