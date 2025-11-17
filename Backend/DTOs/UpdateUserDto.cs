using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UpdateUserDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, StringLength(100)]
        public string FullName { get; set; } = null!;

        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10-11 chữ số")]
        public string PhoneNumber { get; set; } = null!;
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? Address { get; set; }
        public UserRole? Role { get; set; } // Nullable để tránh ghi đè
        public bool? IsActive { get; set; } // Nullable để tránh vô tình khóa tài khoản
    }

}
