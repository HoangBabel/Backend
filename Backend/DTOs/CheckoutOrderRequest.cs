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

        public class CheckoutRentalItemDto
    {
        public int ProductId { get; set; }
        public int RentalDays { get; set; }              // dùng cho /rental-by-days
        public decimal? PricePerDay { get; set; }        // optional, fallback Product.Price
    }

    public class CheckoutRentalByDaysRequest
    {
        [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        public decimal TotalAmount { get; set; }
        public int UserId { get; set; }
        public List<CheckoutRentalItemDto> Items { get; set; } = new();
    }

    public class CheckoutRentalByDatesItemDto
    {
        public int ProductId { get; set; }
        public decimal? PricePerDay { get; set; }        // optional, fallback Product.Price
    }

    public class CheckoutRentalByDatesRequest
    {
        [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        public decimal TotalAmount { get; set; }
        public int UserId { get; set; }
        public DateTime StartDateUtc { get; set; }       // client gửi UTC (khuyên dùng)
        public DateTime EndDateUtc { get; set; }         // exclusive
        public List<CheckoutRentalByDatesItemDto> Items { get; set; } = new();
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
        public string? ServiceType {  get; set; }
        public int? Weight { get; set; }
    }

    public class CheckoutRentalResponse
    {
        public int RentalId { get; set; }
        public int RentalDays { get; set; }
        public string Message { get; set; } = "Tạo đơn thuê thành công";
    }



}
