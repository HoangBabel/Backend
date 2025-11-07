using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class Product : ModelBase
    {
        [Key]
        public int IdProduct { get; set; }

        [Required]
        public int CategoryId { get; set; }  // đủ dùng, không cần [ForeignKey]

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Range(0, 999999999)]
        [Column(TypeName = "decimal(18,2)")] // cố định precision trong DB
        public decimal Price { get; set; }

        [Range(0, 10000)]
        public int Quantity { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(500)]
        public string? Image { get; set; }
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))] // để API nhận/trả enum dạng string
        public ProductStatus Status { get; set; } = ProductStatus.ConHang;
        [JsonIgnore]
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        [Required]
        public bool IsRental { get; set; } = false;  // false = sản phẩm bán, true = chỉ cho thuê

        // Liên kết
        [JsonIgnore] // tránh vòng lặp khi trả Product, và tránh client post Category lồng
        public Category? Category { get; set; }

    }
    public enum ProductStatus
    {
        ConHang = 0,   // Còn hàng
        HetHang = 1    // Hết hàng
    }
    
}
