using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenService _jwt;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext context, IJwtTokenService jwt, IEmailService emailService)
        {
            _context = context;
            _jwt = jwt;
            _emailService = emailService;
        }

        // 🔹 LẤY DANH SÁCH USER (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.Address,
                    u.Role,
                    u.CreatedAt,
                    u.IsActive,
                    u.IsTwoFactorEnabled,
                    u.AvatarUrl
                })
                .ToListAsync();

            return Ok(users);
        }

        // 🔹 LẤY USER THEO ID
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && callerIdStr != id.ToString())
                return Forbid();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.Address,
                    u.Role,
                    u.CreatedAt,
                    u.IsActive,
                    u.IsTwoFactorEnabled,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            return user == null ? NotFound() : Ok(user);
        }

        // 🔹 ĐĂNG KÝ
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var username = dto.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Username == username))
                return BadRequest("Username đã tồn tại.");
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                return BadRequest("Email đã tồn tại.");
            if (!string.IsNullOrEmpty(dto.PhoneNumber) &&
                await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                return BadRequest("Số điện thoại đã tồn tại.");

            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hashed,
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                Address = dto.Address,
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsTwoFactorEnabled = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Tạo giỏ hàng mặc định
            _context.Carts.Add(new Cart
            {
                UserId = user.Id,
                IsCheckedOut = false,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var token = _jwt.CreateToken(user);

            return Ok(new
            {
                Message = "Đăng ký thành công",
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.Role,
                    user.CreatedAt,
                    user.IsActive
                }
            });
        }

        // 🔹 ĐĂNG NHẬP + 2FA
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Sai email hoặc mật khẩu");

            if (user.IsTwoFactorEnabled)
            {
                // Tạo mã OTP 6 chữ số
                var code = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = code;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.Send2FACodeAsync(user.Email, code, user.FullName);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Không thể gửi email: {ex.Message}");
                }

                return Ok(new
                {
                    Message = "Vui lòng kiểm tra email để lấy mã xác thực",
                    RequiresTwoFactor = true,
                    Email = user.Email
                });
            }

            var token = _jwt.CreateToken(user);
            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.Role
                }
            });
        }

        // 🔹 XÁC THỰC 2FA
        [HttpPost("verify-2fa")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

            if (user == null) return Unauthorized("Email không tồn tại");
            if (!user.IsTwoFactorEnabled) return BadRequest("Tài khoản chưa bật 2FA");

            if (user.TwoFactorCode != dto.Code)
                return BadRequest("Mã xác thực không chính xác");
            if (user.TwoFactorCodeExpiry == null || user.TwoFactorCodeExpiry < DateTime.UtcNow)
                return BadRequest("Mã xác thực đã hết hạn");

            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            await _context.SaveChangesAsync();

            var token = _jwt.CreateToken(user);
            return Ok(new
            {
                Message = "Xác thực thành công",
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.Role
                }
            });
        }

        // 🔹 GỬI LẠI MÃ 2FA
        [HttpPost("resend-2fa")]
        [AllowAnonymous]
        public async Task<IActionResult> Resend2FA([FromBody] Resend2FADto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

            if (user == null) return NotFound("Email không tồn tại");
            if (!user.IsTwoFactorEnabled) return BadRequest("Tài khoản chưa bật 2FA");

            var code = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.Send2FACodeAsync(user.Email, code, user.FullName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Không thể gửi email: {ex.Message}");
            }

            return Ok(new { Message = "Đã gửi lại mã xác thực" });
        }

        // 🔹 BẬT/TẮT 2FA
        [HttpPost("toggle-2fa")]
        [Authorize]
        public async Task<IActionResult> Toggle2FA([FromBody] Enable2FADto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userIdStr));
            if (user == null) return NotFound("Không tìm thấy người dùng");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return BadRequest("Mật khẩu không chính xác");

            user.IsTwoFactorEnabled = !user.IsTwoFactorEnabled;
            if (!user.IsTwoFactorEnabled)
            {
                user.TwoFactorCode = null;
                user.TwoFactorCodeExpiry = null;
            }
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = user.IsTwoFactorEnabled ? "Đã bật xác thực 2FA" : "Đã tắt xác thực 2FA",
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            });
        }

        // 🔹 KIỂM TRA TRẠNG THÁI 2FA
        [HttpGet("2fa-status")]
        [Authorize]
        public async Task<IActionResult> Get2FAStatus()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userIdStr));
            if (user == null) return NotFound();

            return Ok(new { user.IsTwoFactorEnabled, user.Email });
        }
        // 🔹 BƯỚC 1: GỬI MÃ XÁC THỰC QUA EMAIL
        [HttpPost("send-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> SendResetCode([FromBody] SendResetCodeDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return BadRequest("Email này không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");

            if (!user.IsActive)
                return BadRequest("Tài khoản đã bị vô hiệu hóa");

            // Tạo mã xác thực 6 chữ số
            var code = new Random().Next(100000, 999999).ToString();
            user.ResetPasswordCode = code;
            user.ResetPasswordCodeExpiry = DateTime.UtcNow.AddMinutes(10); // Hiệu lực 10 phút

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendResetPasswordCodeAsync(user.Email, code, user.FullName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Không thể gửi email: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Mã xác thực đã được gửi đến email của bạn. Mã có hiệu lực trong 10 phút.",
                Email = user.Email
            });
        }

        // 🔹 BƯỚC 2: XÁC THỰC MÃ VÀ ĐẶT LẠI MẬT KHẨU
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return BadRequest("Email này không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");

            if (!user.IsActive)
                return BadRequest("Tài khoản đã bị vô hiệu hóa");

            // Kiểm tra mã xác thực
            if (string.IsNullOrEmpty(user.ResetPasswordCode))
                return BadRequest("Vui lòng yêu cầu gửi mã xác thực trước");

            if (user.ResetPasswordCode != dto.Code)
                return BadRequest("Mã xác thực không chính xác. Vui lòng kiểm tra lại.");

            if (user.ResetPasswordCodeExpiry == null || user.ResetPasswordCodeExpiry < DateTime.UtcNow)
                return BadRequest("Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại mã mới.");

            // Kiểm tra mật khẩu mới không trùng mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                return BadRequest("Mật khẩu mới không được trùng với mật khẩu cũ");

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.ResetPasswordCode = null;
            user.ResetPasswordCodeExpiry = null;

            // Vô hiệu hóa 2FA code nếu có (để bảo mật)
            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới." });
        }

        // 🔹 GỬI LẠI MÃ XÁC THỰC
        [HttpPost("resend-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendResetCode([FromBody] SendResetCodeDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return BadRequest("Email này không tồn tại trong hệ thống. Vui lòng kiểm tra lại.");

            if (!user.IsActive)
                return BadRequest("Tài khoản đã bị vô hiệu hóa");

            // Tạo mã mới
            var code = new Random().Next(100000, 999999).ToString();
            user.ResetPasswordCode = code;
            user.ResetPasswordCodeExpiry = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendResetPasswordCodeAsync(user.Email, code, user.FullName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Không thể gửi email: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Đã gửi lại mã xác thực. Mã có hiệu lực trong 10 phút.",
                Email = user.Email
            });
        }

        // 🔹 ĐỔI MẬT KHẨU (KHI ĐÃ ĐĂNG NHẬP)
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userIdStr));
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            // Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Mật khẩu hiện tại không chính xác");

            // Kiểm tra mật khẩu mới không trùng mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                return BadRequest("Mật khẩu mới không được trùng với mật khẩu hiện tại");

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công" });
        }
        // 🔹 CẬP NHẬT USER
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto input)
        {
            // ✅ 1. Kiểm tra ModelState
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // ✅ 2. Kiểm tra quyền truy cập
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && callerIdStr != id.ToString())
                return Forbid();

            // ✅ 3. Tìm user
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            // ✅ 4. Chuẩn hóa dữ liệu đầu vào
            var normalizedEmail = input.Email?.Trim().ToLowerInvariant();
            var normalizedFullName = input.FullName?.Trim();
            var normalizedPhone = input.PhoneNumber?.Trim();
            var normalizedAddress = input.Address?.Trim();

            // ✅ 5. Validate Email
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return BadRequest("Email không được để trống");

            // ✅ 6. Kiểm tra Email trùng lặp
            if (user.Email.ToLower() != normalizedEmail &&
                await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != id))
                return BadRequest("Email đã tồn tại");

            // ✅ 7. Kiểm tra PhoneNumber trùng lặp (chỉ khi có giá trị)
            if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
                user.PhoneNumber != normalizedPhone &&
                await _context.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone && u.Id != id))
                return BadRequest("Số điện thoại đã tồn tại");

            // ✅ 8. Cập nhật thông tin cơ bản
            user.Email = normalizedEmail;
            user.FullName = normalizedFullName ?? user.FullName;
            user.PhoneNumber = normalizedPhone ?? string.Empty;
            user.Address = normalizedAddress;

            // ✅ 9. Chỉ Admin mới được thay đổi Role/IsActive 
            // (và không được tự thay đổi chính mình)
            if (isAdmin && callerIdStr != id.ToString())
            {
                if (input.Role.HasValue)
                    user.Role = input.Role.Value;
                if (input.IsActive.HasValue)
                    user.IsActive = input.IsActive.Value;
            }

            // ✅ 10. Lưu thay đổi
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật: {ex.Message}");
            }

            // ✅ 11. Trả về kết quả
            return Ok(new
            {
                Message = "Cập nhật thành công",
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.Role,
                    user.IsActive,
                    user.AvatarUrl
                }
            });
        }


        // 🔹 XÓA USER (Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (callerIdStr == id.ToString()) return BadRequest("Không thể xóa chính mình");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok("Xóa thành công");
        }

        // 🔹 LẤY USER HIỆN TẠI
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == int.Parse(userIdStr))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.PhoneNumber,
                    u.Address,
                    u.Role,
                    u.IsTwoFactorEnabled,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            return user == null ? NotFound() : Ok(user);
        }

        // 🔹 UPLOAD AVATAR
        [HttpPost("{id:int}/avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile avatar)
        {
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && callerIdStr != id.ToString()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng");
            if (avatar == null || avatar.Length == 0) return BadRequest("File không hợp lệ");

            var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await avatar.CopyToAsync(stream);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            user.AvatarUrl = $"{baseUrl}/uploads/avatars/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tải lên thành công", user.AvatarUrl });
        }
    }
}
