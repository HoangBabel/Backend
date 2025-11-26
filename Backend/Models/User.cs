using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Backend.Models.Backend.Models;

namespace Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10-11 chữ số")]
        public string PhoneNumber { get; set; } = null!;
        // ✅ THÊM CÁC TRƯỜNG MỚI CHO RESET PASSWORD
        public string? ResetPasswordCode { get; set; }
        public DateTime? ResetPasswordCodeExpiry { get; set; }
        // 🏠 Địa chỉ người dùng
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? Address { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; } = UserRole.Customer;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;

        // 🧩 Ảnh đại diện người dùng
        [StringLength(255)]
        public string? AvatarUrl { get; set; }

        // ===== Two-Factor Authentication =====
        public bool IsTwoFactorEnabled { get; set; } = false;

        [StringLength(6)]
        public string? TwoFactorCode { get; set; }

        public DateTime? TwoFactorCodeExpiry { get; set; }

        // Số lần nhập sai 2FA
        public int TwoFactorAttemptCount { get; set; } = 0;

        // 🔗 Quan hệ
        [JsonIgnore]
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        [JsonIgnore]
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

    }

    public enum UserRole
    {
        Admin,
        Staff,
        Customer,
        Shipper
    }
}
