namespace Backend.DTOs
{
    public enum PaymentMethod
    {
        COD = 0,
        QR = 1
    }
    public class CheckoutOrderRequest
    {
        public int UserId { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    }

    public class CheckoutRentalItemDto
    {
        public int ProductId { get; set; }
        public int RentalDays { get; set; }              // dùng cho /rental-by-days
        public decimal? PricePerDay { get; set; }        // optional, fallback Product.Price
    }

    public class CheckoutRentalByDaysRequest
    {
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
        public int UserId { get; set; }
        public DateTime StartDateUtc { get; set; }       // client gửi UTC (khuyên dùng)
        public DateTime EndDateUtc { get; set; }         // exclusive
        public List<CheckoutRentalByDatesItemDto> Items { get; set; } = new();
    }

    public class CheckoutOrderResponse
    {
        public int OrderId { get; set; }
        public string Message { get; set; } = "Đặt hàng thành công!";
    }

    public class CheckoutRentalResponse
    {
        public int RentalId { get; set; }
        public int RentalDays { get; set; }
        public string Message { get; set; } = "Tạo đơn thuê thành công";
    }

}
