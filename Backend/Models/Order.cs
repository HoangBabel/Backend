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

        // ===== THÊM CÁC TRƯỜNG MỚI CHO SHIPPING =====

        // Thông tin địa chỉ chi tiết để tính phí ship
        public int? ToProvinceId { get; set; }

        [StringLength(100)]
        public string? ToProvinceName { get; set; }

        public int? ToDistrictId { get; set; }

        [StringLength(100)]
        public string? ToDistrictName { get; set; }

        [StringLength(20)]
        public string? ToWardCode { get; set; }

        [StringLength(100)]
        public string? ToWardName { get; set; }

        // Thông tin giao hàng
        public int? ServiceId { get; set; } = 53321; // Mặc định: GHN Tiết Kiệm

        [StringLength(50)]
        public string? ServiceType { get; set; }

        // Thông tin kích thước/trọng lượng
        public int? Weight { get; set; } // gram
        public int? Length { get; set; } // cm
        public int? Width { get; set; }  // cm
        public int? Height { get; set; } // cm

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
