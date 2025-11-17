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

        [Required, ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

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

        // ===== SHIPPING INFO =====
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

        public int? ServiceId { get; set; } = 53321; // GHN Tiết Kiệm mặc định
        [StringLength(50)]
        public string? ServiceType { get; set; }

        public int? Weight { get; set; }
        public int? Length { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        // ===== VOUCHER =====
        public int? VoucherId { get; set; }
        public Vouncher? Voucher { get; set; }

        [StringLength(50)]
        public string? VoucherCodeSnapshot { get; set; }

        // ===== PAYOS INTEGRATION =====
        [StringLength(100)]
        public string? PaymentLinkId { get; set; }

        [StringLength(500)]
        public string? PaymentUrl { get; set; }

        [StringLength(500)]
        public string? QrCodeUrl { get; set; }

        [StringLength(50)]
        public string? TransactionCode { get; set; }

        [StringLength(100)]
        public string? PaymentStatus { get; set; } = "UNPAID"; // PAID | FAILED | PENDING | UNPAID

        public DateTime? PaidAt { get; set; } // thời điểm thanh toán thành công

        // ===== ITEMS =====
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }
}
