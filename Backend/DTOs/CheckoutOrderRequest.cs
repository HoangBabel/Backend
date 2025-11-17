using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public enum PaymentMethod
    {
        COD = 0,
        QR = 1
    }
    public class CheckoutOrderRequest
    {
        [Required, StringLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        // ===== THÊM THÔNG TIN ĐỊA CHỈ CHO SHIPPING =====
        [Required]
        public int ToProvinceId { get; set; }

        public string? ToProvinceName { get; set; }

        [Required]
        public int ToDistrictId { get; set; }

        public string? ToDistrictName { get; set; }

        [Required]
        public string ToWardCode { get; set; } = string.Empty;

        public string? ToWardName { get; set; }

        // ===== THÔNG TIN GIAO HÀNG =====
        public int ServiceId { get; set; } = 53321; // Mặc định: GHN Tiết Kiệm

        public int? Weight { get; set; } // Nếu null sẽ tính từ sản phẩm
        public int? Length { get; set; } = 20;
        public int? Width { get; set; } = 20;
        public int? Height { get; set; } = 20;

        // ===== CÁC TRƯỜNG CŨ =====
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        public string? VoucherCode { get; set; }
    }
    public class CheckoutOrderResponse
    {
        public string Message { get; set; } = "Đặt hàng thành công.";
        public int OrderId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? VoucherCode { get; set; }
        public string? ServiceType { get; set; }
        public int? Weight { get; set; }

        // ✅ Thêm các trường cho FE lấy Payment/QR
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
        public string? PaymentLinkId { get; set; }
    }



}
