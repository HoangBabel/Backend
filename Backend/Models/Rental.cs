using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Backend.Models
{
    public class Rental
    {
        //[Key]
        //public int Id { get; set; }

        //[Required]
        //public int UserId { get; set; }
        //[JsonIgnore]                // không bind/serialize
        //[ValidateNever]             // không validate
        //public User? User { get; set; }   // <- cho nullable

        //public DateTime StartDate { get; set; }  // Ngày bắt đầu thuê
        //public DateTime EndDate { get; set; }    // Ngày kết thúc thuê

        //[Required]
        //[Column(TypeName = "nvarchar(20)")]
        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public RentalStatus Status { get; set; } = RentalStatus.Pending;

        //public ICollection<RentalItem> Items { get; set; } = new List<RentalItem>();
        //[Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; private set; } // chỉ tiền thuê
        //public DateTime? ReturnedAt { get; set; }

        //[Column(TypeName = "decimal(18,2)")] public decimal DepositPaid { get; private set; }
        //[Column(TypeName = "decimal(18,2)")] public decimal LateFee { get; private set; }
        //[Column(TypeName = "decimal(18,2)")] public decimal CleaningFee { get; private set; }
        //[Column(TypeName = "decimal(18,2)")] public decimal DamageFee { get; private set; }
        //[Column(TypeName = "decimal(18,2)")] public decimal DepositRefund { get; private set; }

        //public void RecalculateTotal() => TotalPrice = Items.Sum(i => i.SubTotal);
        //public void SnapshotDepositFromItems() => DepositPaid = Items.Sum(i => i.DepositAtBooking ?? 0m);
        //public void SetSettlement(DateTime returnedAt, decimal lateFee, decimal cleaningFee, decimal damageFee) { ... }
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [JsonIgnore]
        public User? User { get; set; }

        // Khuyến nghị lưu UTC ở backend
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RentalStatus Status { get; set; } = RentalStatus.Pending;

        public DateTime? ReturnedAt { get; private set; }

        public ICollection<RentalItem> Items { get; set; } = new List<RentalItem>();

        // ===== Tổng & phí (server-side only) =====
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; private set; } // chỉ tiền thuê (sum SubTotal)

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositPaid { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CleaningFee { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DamageFee { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositRefund { get; private set; }

        // (tuỳ chọn) chống cập nhật ghi đè khi nhiều request song song
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ====== Helpers ======

        public void EnsureValidDateRange()
        {
            if (EndDate <= StartDate)
                throw new InvalidOperationException("EndDate phải lớn hơn StartDate.");
        }

        public void RecalculateTotal()
        {
            TotalPrice = Items.Sum(i => i.SubTotal);
        }

        public void SnapshotDepositFromItems()
        {
            DepositPaid = Items.Sum(i => (i.DepositAtBooking ?? 0m) * i.Quantity);
        }

        public void Activate()
        {
            if (Status != RentalStatus.Pending)
                throw new InvalidOperationException("Chỉ kích hoạt khi đơn đang ở trạng thái Pending.");
            Status = RentalStatus.Active;
        }

        public void Cancel(string? reason = null)
        {
            if (Status == RentalStatus.Completed)
                throw new InvalidOperationException("Đơn đã hoàn tất không thể huỷ.");
            Status = RentalStatus.Cancelled;
            // (tuỳ chọn) lưu reason vào một field nếu bạn thêm
        }

        /// <summary>
        /// Quyết toán khi trả hàng: tính hoàn cọc = DepositPaid - (Late + Cleaning + Damage).
        /// </summary>
        public void SetSettlement(DateTime returnedAt, decimal lateFee, decimal cleaningFee, decimal damageFee)
        {
            if (Status is not (RentalStatus.Pending or RentalStatus.Active))
                throw new InvalidOperationException("Chỉ quyết toán đơn ở trạng thái Pending hoặc Active.");

            ReturnedAt = returnedAt;
            LateFee = Math.Max(0m, lateFee);
            CleaningFee = Math.Max(0m, cleaningFee);
            DamageFee = Math.Max(0m, damageFee);

            var deductions = LateFee + CleaningFee + DamageFee;
            DepositRefund = Math.Max(0m, DepositPaid - deductions);

            Status = RentalStatus.Completed;
        }
    }
    public enum RentalStatus
    {
        Pending,     // Đang chờ xác nhận
        Active,      // Đang thuê
        Completed,   // Đã trả
        Cancelled    // Đã hủy
    }
}
