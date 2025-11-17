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

        // 🔹 CẬP NHẬT USER
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto input)
        {
            var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && callerIdStr != id.ToString()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Email != input.Email &&
                await _context.Users.AnyAsync(u => u.Email == input.Email && u.Id != id))
                return BadRequest("Email đã tồn tại");
            if (!string.IsNullOrEmpty(input.PhoneNumber) &&
                user.PhoneNumber != input.PhoneNumber &&
                await _context.Users.AnyAsync(u => u.PhoneNumber == input.PhoneNumber && u.Id != id))
                return BadRequest("Số điện thoại đã tồn tại");

            user.Email = input.Email;
            user.FullName = input.FullName;
            user.PhoneNumber = input.PhoneNumber;
            user.Address = input.Address;

            if (isAdmin)
            {
                if (input.Role.HasValue) user.Role = input.Role.Value;
                if (input.IsActive.HasValue) user.IsActive = input.IsActive.Value;
            }

            await _context.SaveChangesAsync();
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
                    user.IsActive
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
