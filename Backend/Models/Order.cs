using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Backend.DTOs;

namespace Backend.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;

        // Ngày đặt hàng
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Tổng tiền
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;


        // Trạng thái đơn hàng
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Địa chỉ giao hàng
        [Required]
        [StringLength(255)]
        public string? ShippingAddress { get; set; }


        // Danh sách sản phẩm trong đơn
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
