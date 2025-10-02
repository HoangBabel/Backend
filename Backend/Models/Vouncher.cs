using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{

    public class Vouncher
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        // Loại giảm giá: "Fixed", "Percent", "Shipping"
        public string Type { get; set; } = "Percent";

        public int? DiscountValue { get; set; }        // Giảm trực tiếp (Fixed)
        public int? DiscountPercent { get; set; }      // % giảm giá
        public int? MaximumDiscount { get; set; }      // Giảm tối đa khi tính %

        public int MinimumOrderValue { get; set; } = 0;
        public bool ApplyToShipping { get; set; } = false;

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        public bool IsValid { get; set; } = true;

        public DateTime? UsedAt { get; set; }
        public int MaxUsageCount { get; set; } = 1;
        public int CurrentUsageCount { get; set; } = 0;
    }
}
