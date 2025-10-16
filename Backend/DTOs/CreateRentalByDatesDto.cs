namespace Backend.DTOs
{
    public class CreateRentalByDatesDto
    {
        public int UserId { get; set; }
        public DateTime StartDate { get; set; } // client gửi ISO, ví dụ 2025-10-20
        public DateTime EndDate { get; set; }   // ngày trả (exclusive hoặc inclusive, xem dưới)
        public List<CreateRentalItemDto> Items { get; set; } = new();
    }
}
