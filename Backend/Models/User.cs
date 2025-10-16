using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class User
    {
        [Key] // Khóa chính
        public int Id { get; set; }

        // Tài khoản đăng nhập
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        // Mật khẩu hash (không lưu plaintext)
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = null!;

        // Thông tin cá nhân
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10-11 chữ số")]
        public string PhoneNumber { get; set; } = null!;

        // Vai trò trong hệ thống
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; } = UserRole.Customer;

        // Thời gian tạo
        [Required]
        public DateTime CreatedAt { get; set; }
        [JsonIgnore]
        public Cart Cart { get; set; } = null!;
        // Trạng thái hoạt động
        [Required]
        public bool IsActive { get; set; } = true;
    }

    //0,1,2,3
    public enum UserRole
    {
        Admin,
        Staff,
        Customer,
        Shipper
    }
}

