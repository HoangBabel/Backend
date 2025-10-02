using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwt;

    public UserController(AppDbContext context, IJwtTokenService jwt)
    {
        _context = context;
        _jwt = jwt;
    }
    // Lấy danh sách user
    [HttpGet]
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
                u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    // Lấy user theo Id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
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
                u.IsActive
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();
        return Ok(user);
    }

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
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.Carts.Add(new Cart { UserId = user.Id, IsCheckedOut = false, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // (tuỳ chọn) tự đăng nhập sau khi đăng ký:
        var token = _jwt.CreateToken(user);

        return Ok(new
        {
            Message = "Đăng ký thành công",
            Token = token,
            User = new { user.Id, user.Username, user.Email, user.FullName, user.PhoneNumber, user.Role, user.CreatedAt, user.IsActive }
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);
        if (user == null) return Unauthorized("Sai email hoặc mật khẩu");

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!ok) return Unauthorized("Sai email hoặc mật khẩu");

        var token = _jwt.CreateToken(user);

        return Ok(new
        {
            Message = "Đăng nhập thành công",
            Token = token,
            User = new { user.Id, user.Username, user.Email, user.FullName, user.PhoneNumber, user.Role }
        });
    }


    // Cập nhật user
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] User userInput)
    {
        var callerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && callerIdStr != id.ToString()) return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Email = userInput.Email;
        user.FullName = userInput.FullName;
        user.PhoneNumber = userInput.PhoneNumber;
        user.Role = isAdmin ? userInput.Role : user.Role; // chỉ Admin đổi Role
        user.IsActive = isAdmin ? userInput.IsActive : user.IsActive;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok("Cập nhật thành công");
    }

    // Xóa user
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Xóa thành công");
    }
}