namespace Backend.Models
{
    public sealed class Payment
    {
        public int Id { get; set; }
        public string? PaymentLinkId { get; set; }
        public long OrderCode { get; set; }          // <— lưu orderCode đã gửi PayOS
        public PaymentType Type { get; set; }        // Order / Rental
        public int RefId { get; set; }               // Id của Order hoặc Rental
        public long ExpectedAmount { get; set; }
        public PaymentStatus Status { get; set; }    // Created / Paid / ...
        public string? QrCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastEventAt { get; set; }
        public string? RawPayload { get; set; }      // tận dụng lưu CheckoutUrl
        public string? Description { get; set; }     // "RENTAL", "ORDER" (tuỳ bạn)
    }
    public enum PaymentType
    {
        Order = 1,
        Rental = 2
    }

    public enum PaymentStatus
    {
        Created = 1,
        Paid = 2,
        Failed = 3,
        Cancelled = 4
    }

}
