using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    // Lấy danh sách user
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // Lấy user theo Id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // Đăng ký (tạo user mới)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        // Kiểm tra trùng username
        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            return BadRequest("Username đã tồn tại.");

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            return BadRequest("Email đã tồn tại.");

        user.CreatedAt = DateTime.Now;
        user.IsActive = true;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Đăng ký thành công");
    }

    // Đăng nhập
    // Đăng nhập bằng Email + Password
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Vui lòng nhập email và mật khẩu");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email
                                   && u.PasswordHash == request.Password // ⚠️ nên hash mật khẩu
                                   && u.IsActive);

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        var result = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role
        };

        return Ok(new { Message = "Đăng nhập thành công", User = result });
    }

    // Cập nhật user
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] User user)
    {
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null) return NotFound();

        existingUser.Email = user.Email;
        existingUser.FullName = user.FullName;
        existingUser.PhoneNumber = user.PhoneNumber;
        existingUser.Role = user.Role;
        existingUser.IsActive = user.IsActive;

        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync();

        return Ok("Cập nhật thành công");
    }

    // Xóa user
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Xóa thành công");
    }
}