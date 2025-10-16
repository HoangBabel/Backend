using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class RentalItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RentalId { get; set; }
        [JsonIgnore]
        public Rentals Rental { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Range(1, 365)]
        public int RentalDays { get; set; }  // Số ngày thuê

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerDay { get; set; }  // Giá thuê mỗi ngày

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }  // Thành tiền = Giá * Số ngày
    }
}
