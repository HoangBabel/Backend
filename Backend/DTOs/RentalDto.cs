using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RentalDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = null!;
        public string? PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositPaid { get; set; }
        public List<RentalItemDto> Items { get; set; } = new();
    }

    public class RentalItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public int Units { get; set; }
        public decimal PricePerUnitAtBooking { get; set; }
        public decimal SubTotal { get; set; }
    }

}
