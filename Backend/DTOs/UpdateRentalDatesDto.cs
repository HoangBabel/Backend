using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UpdateRentalDatesDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class UpdateRentalStatusDto
    {
        [Required]
        public RentalStatus Status { get; set; }
    }

    public class AdminRentalDto
    {
        public int Id { get; set; }

        // 👤 Người thuê
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        // 📦 Trạng thái
        public string Status { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }

        // ⏱ Thời gian
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ReturnedAt { get; set; }

        // 💰 Tài chính
        public decimal TotalPrice { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal LateFee { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal DamageFee { get; set; }
        public decimal DepositRefund { get; set; }

        // 📄 Chi tiết
        public List<RentalItemDto> Items { get; set; } = new();
    }

}
