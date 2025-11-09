using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class RentalPlan
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public RentalUnit Unit { get; set; } = RentalUnit.Day;
        [Column(TypeName = "decimal(18,2)")] 
        public decimal PricePerUnit { get; set; }
        public int MinUnits { get; set; } = 1;
        [Column(TypeName = "decimal(18,2)")] 
        public decimal? Deposit { get; set; }
        [Column(TypeName = "decimal(18,2)")] 
        public decimal? LateFeePerUnit { get; set; }
    }
    public enum RentalUnit { Day, Week, Month }
    public class RentalPricingTier
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ThresholdDays { get; set; } // áp dụng khi days >= threshold
        [Column(TypeName = "decimal(18,2)")] public decimal PricePerDay { get; set; }
    }
}
