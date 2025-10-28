using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class Enable2FADto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = null!;
    }

    public class Verify2FADto
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực phải có 6 chữ số")]
        public string Code { get; set; } = null!;
    }

    public class Resend2FADto
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
