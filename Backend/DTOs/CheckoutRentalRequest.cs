namespace Backend.DTOs
{
    public class CheckoutRentalRequest
    {
        public int RentalId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        // Shipping info
        public string ShippingAddress { get; set; } = string.Empty;
        public int ToProvinceId { get; set; }
        public string ToProvinceName { get; set; } = string.Empty;
        public int ToDistrictId { get; set; }
        public string ToDistrictName { get; set; } = string.Empty;
        public string ToWardCode { get; set; } = string.Empty;
        public string ToWardName { get; set; } = string.Empty;
        public int ServiceId { get; set; }

        // Package dimensions (optional)
        public int? Weight { get; set; }
        public int? Length { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        // Voucher
        public string? VoucherCode { get; set; }
    }

    public class CheckoutRentalResponse
    {
        public string Message { get; set; } = "Thanh toán đơn thuê thành công.";
        public int RentalId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Deposit { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? VoucherCode { get; set; }
        public string? ServiceType { get; set; }
        public int Weight { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
        public string? PaymentLinkId { get; set; }
    }

}
