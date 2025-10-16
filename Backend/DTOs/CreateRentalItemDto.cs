namespace Backend.DTOs
{
    public class CreateRentalItemDto
    {
        public int ProductId { get; set; }
        public decimal? PricePerDay { get; set; } // null -> mặc định từ Product.Price
    }
}
