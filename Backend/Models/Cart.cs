using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models
{
   
        public class Cart
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public int UserId { get; set; }
            [JsonIgnore]
            public User User { get; set; } = null!;

            public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

            public DateTime UpdatedAt { get; set; }
            public bool IsCheckedOut { get; set; } = false; // true nếu đã chuyển thành Order
        }

    }

