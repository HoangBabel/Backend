using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class Rentals
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;

        public DateTime StartDate { get; set; }  // Ngày bắt đầu thuê
        public DateTime EndDate { get; set; }    // Ngày kết thúc thuê

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RentalStatus Status { get; set; } = RentalStatus.Pending;

        public ICollection<RentalItem> Items { get; set; } = new List<RentalItem>();
    }

    public enum RentalStatus
    {
        Pending,     // Đang chờ xác nhận
        Active,      // Đang thuê
        Completed,   // Đã trả
        Cancelled    // Đã hủy
    }
}
