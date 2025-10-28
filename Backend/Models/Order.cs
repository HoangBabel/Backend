using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Backend.DTOs;

namespace Backend.Models
{
    public class Order
    {
        [Key] public int Id { get; set; }

        [Required, ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required] public DateTime OrderDate { get; set; } = DateTime.Now;

        // Tổng tiền hàng (chưa giảm, chưa phí ship)
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Phí vận chuyển (nếu có)
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0m;

        // Số tiền giảm từ voucher
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0m;

        // Tổng thanh toán cuối (TotalAmount + ShippingFee - DiscountAmount)
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        [Required, Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        [Required, Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required, StringLength(255)]
        public string? ShippingAddress { get; set; }

        // Gắn voucher (nullable, để order không bắt buộc có voucher)
        public int? VoucherId { get; set; }
        public Vouncher? Voucher { get; set; }

        // Lưu ảnh chụp mã voucher lúc đặt (phòng khi giá trị voucher đổi về sau)
        [StringLength(50)]
        public string? VoucherCodeSnapshot { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
    public enum OrderStatus
    {
        Pending,     // Đang chờ xử lý
        Processing,  // Đang xử lý
        Completed,   // Hoàn tất
        Cancelled    // Hủy
    }
}
