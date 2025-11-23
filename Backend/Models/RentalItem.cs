using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Org.BouncyCastle.Utilities;

namespace Backend.Models
{
    public class RentalItem
    {
      
        [Key]
        public int Id { get; set; }

        // FK
        [Required]
        public int RentalId { get; set; }
        [JsonIgnore]
        public Rental? Rental { get; set; }

        [Required]
        public int ProductId { get; set; }
        [JsonIgnore]
        public Product Product { get; set; } = null!;

        // ✅ số lượng thiết bị thuê (mặc định 1)
        public int Quantity { get; set; } = 1;


        // ==== Snapshot pricing tại thời điểm đặt (theo ngày) ====
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerUnitAtBooking { get; private set; }  // giá/ngày đã chốt

        // Số đơn vị thuê (ngày). Set từ backend = Ceil((End-Start).TotalDays)
        [Range(1, 365)]
        public int Units { get; private set; }

        // Thành tiền của dòng (server-side): PricePerUnitAtBooking * Units
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; private set; }

        // Cọc & phí trễ đã snapshot (mỗi ngày trễ)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DepositAtBooking { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LateFeePerUnitAtBooking { get; private set; }
      
        // ===== Methods (server-side only) =====
        public void SnapshotPricing(decimal pricePerUnit, decimal? deposit = null, decimal? lateFeePerUnit = null)
        {
            if (pricePerUnit < 0) throw new ArgumentOutOfRangeException(nameof(pricePerUnit));
            if (deposit is < 0) throw new ArgumentOutOfRangeException(nameof(deposit));
            if (lateFeePerUnit is < 0) throw new ArgumentOutOfRangeException(nameof(lateFeePerUnit));

            PricePerUnitAtBooking = pricePerUnit;
            DepositAtBooking = deposit;
            LateFeePerUnitAtBooking = lateFeePerUnit;

            // giữ SubTotal nhất quán nếu Units đã có
            RecalculateSubTotal();
        }


        public void SetUnits(int units)
        {
            if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
            Units = units;
            RecalculateSubTotal();
        }

        public void SetQuantity(int qty)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            Quantity = qty;
            RecalculateSubTotal();
        }

        private void RecalculateSubTotal()
        {
            if (PricePerUnitAtBooking <= 0 || Units <= 0 || Quantity <= 0)
            {
                SubTotal = 0m;
                return;
            }

            // ✅ tiền thuê = giá/ngày * số ngày * số lượng
            SubTotal = Math.Round(
                PricePerUnitAtBooking * Units * Quantity,
                2,
                MidpointRounding.AwayFromZero
            );
        }
    }
}
