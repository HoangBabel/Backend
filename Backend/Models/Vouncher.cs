using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models
{

    public class Vouncher
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

      
        public string? Type { get; set; }

        public decimal? DiscountValue { get; set; }        // Giảm trực tiếp (Fixed)
        public decimal? DiscountPercent { get; set; }      // % giảm giá
        public decimal? MaximumDiscount { get; set; }      // Giảm tối đa khi tính %

        public decimal MinimumOrderValue { get; set; } = 0;
        public bool ApplyToShipping { get; set; } = false;

        [Required]
        public DateTime ExpirationDate { get; set; }
        // ✅ THÊM MỚI
        /// <summary>
        /// % giảm phí ship - VD: 50 = giảm 50% ship, null = miễn phí 100%
        /// Chỉ dùng khi Type = "Shipping" và ApplyToShipping = true
        /// </summary>
        public decimal? ShippingDiscountPercent { get; set; }
        [Required]
        public bool IsValid { get; set; } = true;

        public DateTime? UsedAt { get; set; }
        public int MaxUsageCount { get; set; } = 1;
        public int CurrentUsageCount { get; set; } = 0;
        [JsonIgnore]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [JsonIgnore]
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    }
}
