using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    
    //  LẤY DANH SÁCH USER
   
    [HttpGet]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới xem được danh sách
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
                u.Role,
                u.CreatedAt,
                u.IsActive,
                u.IsTwoFactorEnabled // ✅ Thêm trạng thái 2FA
            })
            .ToListAsync();

        return Ok(users);
    }

  
    //  LẤY USER THEO ID
    
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        // Chỉ Admin hoặc chính user đó mới xem được
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
                u.Role,
                u.CreatedAt,
                u.IsActive,
                u.IsTwoFactorEnabled
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }

    
    //  ĐĂNG KÝ

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
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsTwoFactorEnabled = false // Mặc định tắt 2FA
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Tạo giỏ hàng cho user mới
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
                user.Role,
                user.CreatedAt,
                user.IsActive
            }
        });
    }

  
    //  ĐĂNG NHẬP (DUY NHẤT - CÓ HỖ TRỢ 2FA)
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Sai email hoặc mật khẩu");

        // ✅ KIỂM TRA 2FA
        if (user.IsTwoFactorEnabled)
        {
            // Tạo mã 6 số ngẫu nhiên
            var code = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);

            await _context.SaveChangesAsync();

            // Gửi email
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

        // Nếu KHÔNG bật 2FA → đăng nhập bình thường
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
                user.Role
            }
        });
    }

 
    //  XÁC THỰC MÃ 2FA
    
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user == null)
            return Unauthorized("Email không tồn tại");

        // Kiểm tra mã
        if (user.TwoFactorCode != dto.Code)
            return BadRequest("Mã xác thực không chính xác");

        // Kiểm tra hết hạn
        if (user.TwoFactorCodeExpiry == null || user.TwoFactorCodeExpiry < DateTime.UtcNow)
            return BadRequest("Mã xác thực đã hết hạn");

        // Xóa mã sau khi xác thực thành công
        user.TwoFactorCode = null;
        user.TwoFactorCodeExpiry = null;
        await _context.SaveChangesAsync();

        // Tạo token
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
                user.Role
            }
        });
    }

    
    //  GỬI LẠI MÃ 2FA
   
    [HttpPost("resend-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> Resend2FA([FromBody] Resend2FADto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user == null)
            return NotFound("Email không tồn tại");

        if (!user.IsTwoFactorEnabled)
            return BadRequest("Tài khoản chưa bật xác thực 2 yếu tố");

        // Tạo mã mới
        var code = new Random().Next(100000, 999999).ToString();
        user.TwoFactorCode = code;
        user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);

        await _context.SaveChangesAsync();

        // Gửi email
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

  
    //  BẬT/TẮT 2FA
    
    [HttpPost("toggle-2fa")]
    [Authorize]
    public async Task<IActionResult> Toggle2FA([FromBody] Enable2FADto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

        var userId = int.Parse(userIdStr);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("Không tìm thấy người dùng");

        // Xác thực mật khẩu
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return BadRequest("Mật khẩu không chính xác");

        // Toggle 2FA
        user.IsTwoFactorEnabled = !user.IsTwoFactorEnabled;

        // Xóa mã cũ nếu tắt 2FA
        if (!user.IsTwoFactorEnabled)
        {
            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = user.IsTwoFactorEnabled
                ? "Đã bật xác thực 2 yếu tố"
                : "Đã tắt xác thực 2 yếu tố",
            IsTwoFactorEnabled = user.IsTwoFactorEnabled
        });
    }

    
    //  KIỂM TRA TRẠNG THÁI 2FA
    
    [HttpGet("2fa-status")]
    [Authorize]
    public async Task<IActionResult> Get2FAStatus()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

        var userId = int.Parse(userIdStr);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            IsTwoFactorEnabled = user.IsTwoFactorEnabled,
            Email = user.Email
        });
    }

    
    //  CẬP NHẬT USER
  
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto userInput)
    {
        var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && callerIdStr != id.ToString())
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Kiểm tra email trùng
        if (user.Email != userInput.Email &&
            await _context.Users.AnyAsync(u => u.Email == userInput.Email && u.Id != id))
            return BadRequest("Email đã tồn tại");

        // Kiểm tra phone trùng
        if (!string.IsNullOrEmpty(userInput.PhoneNumber) &&
            user.PhoneNumber != userInput.PhoneNumber &&
            await _context.Users.AnyAsync(u => u.PhoneNumber == userInput.PhoneNumber && u.Id != id))
            return BadRequest("Số điện thoại đã tồn tại");

        user.Email = userInput.Email;
        user.FullName = userInput.FullName;
        user.PhoneNumber = userInput.PhoneNumber;

        // Chỉ Admin mới đổi Role và IsActive
        if (isAdmin)
        {
            user.Role = userInput.Role;
            user.IsActive = userInput.IsActive;
        }

        _context.Users.Update(user);
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
                user.Role,
                user.IsActive
            }
        });
    }

  
    // XÓA USER
   
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Không cho xóa chính mình
        var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (callerIdStr == id.ToString())
            return BadRequest("Không thể xóa chính mình");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Xóa thành công");
    }
}
