namespace Backend.DTOs
{
    public class CheckoutRentalRequest
    {
        public int RentalId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.QR;
    }

    public class CheckoutRentalResponse
    {
        public int RentalId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Deposit { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Message { get; set; } = "Đặt thuê thành công.";

        // Thông tin PayOS (nếu tạo)
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
        public string? PaymentLinkId { get; set; }
    }
}
