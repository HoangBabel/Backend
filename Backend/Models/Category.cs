using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;   // non-nullable + null-forgivinga

        // Quan hệ 1-nhiều
        [JsonIgnore] // tránh vòng lặp khi trả về Category
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
